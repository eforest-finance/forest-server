using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Grains;
using NFTMarketServer.Grains.Grain.Order;
using NFTMarketServer.Options;
using NFTMarketServer.Order.Dto;
using NFTMarketServer.Order.Etos;
using NFTMarketServer.Order.Index;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Users.Index;
using NFTMarketServer.Users.Provider;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;
using GuidHelper = NFTMarketServer.Helper.GuidHelper;

namespace NFTMarketServer.Order;

[RemoteService(IsEnabled = false)]
public class OrderAppService : ApplicationService, IOrderAppService
{
    private const string SeedCachePrefix = "NFTMarketServer:Order:SeedCache:";
    private const string SeedLockPrefix = "NFTMarketServer:Order:SeedLock:";
    private readonly IDistributedCache<string> _seedLockCache;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IPortkeyClientProvider _portkeyClientProvider;
    private readonly IOptionsMonitor<PortkeyOption> _portkeyOptionMonitor;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INESTRepository<NFTOrderIndex, Guid> _nftOrderIndexRepository;

    public OrderAppService(IDistributedCache<string> seedLockCache, IAbpDistributedLock distributedLock, 
        IClusterClient clusterClient, IObjectMapper objectMapper, IDistributedEventBus distributedEventBus, 
        IUserInformationProvider userInformationProvider, IPortkeyClientProvider portkeyClientProvider, 
        IOptionsMonitor<PortkeyOption> portkeyOptionMonitor, IGraphQLProvider graphQlProvider, INESTRepository<NFTOrderIndex, Guid> nftOrderIndexRepository)
    {
        _seedLockCache = seedLockCache;
        _distributedLock = distributedLock;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _userInformationProvider = userInformationProvider;
        _portkeyClientProvider = portkeyClientProvider;
        _portkeyOptionMonitor = portkeyOptionMonitor;
        _graphQlProvider = graphQlProvider;
        _nftOrderIndexRepository = nftOrderIndexRepository;
    }

    public async Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderInput input)
    {
        AssertSeedSymbol(input.Symbol);
        var result = new CreateOrderResultDto();
        AssertHelper.IsTrue(String.IsNullOrWhiteSpace(await _seedLockCache.GetAsync(SeedCachePrefix + input.Symbol)), "symbol locked");
        var seedInfoDto = await _graphQlProvider.GetSeedInfoAsync(input.Symbol);
        AssertHelper.IsTrue(seedInfoDto.Status == SeedStatus.AVALIABLE && seedInfoDto.SeedType == SeedType.Regular, "invalid symbol");
        var userIndex = await GetUserIndexAsync();
        AssertHelper.NotNull(userIndex, "not login");
        await using var handle = 
            await _distributedLock.TryAcquireAsync(SeedLockPrefix + input.Symbol);
        if (handle != null)
        {
            var nftOrder = new NFTOrder
            {
                NftSymbol = input.Symbol,
                PaymentSymbol = seedInfoDto.TokenPrice.Symbol,
                PaymentAmount = seedInfoDto.TokenPrice.Amount,
                UserId = userIndex.Id,
                Address = userIndex.AelfAddress,
                ChainId = GetChainId(userIndex, OrderConstants.DefaultChainId),
                Network = OrderConstants.DefaultNetWork,
                MerchantName = _portkeyOptionMonitor.CurrentValue.Name,
                WebhookUrl = _portkeyOptionMonitor.CurrentValue.CallBackUrl,
                CreateTime = DateTime.UtcNow.ToTimestamp().Seconds,
                LastModifyTime = DateTime.UtcNow.ToTimestamp().Seconds,
                OrderStatus = OrderStatus.Init
            };
            await AddOrUpdateNFTOrderAsync(nftOrder);

            var createOrderParam = _objectMapper.Map<NFTOrder, PortkeyCreateOrderParam>(nftOrder);
            createOrderParam.CaHash = userIndex.CaHash;
            createOrderParam.MerchantName = OrderConstants.LocalMerchantName;
            createOrderParam.MerchantOrderId = nftOrder.Id;
            createOrderParam.PaymentAmount = nftOrder.PaymentAmount.ToString();
            createOrderParam.PaymentSymbol = nftOrder.PaymentSymbol;
            var createOrderResult = await _portkeyClientProvider.CreateOrderAsync(createOrderParam);
            AssertHelper.NotNull(createOrderResult, "portkey create order fail");
            nftOrder.ThirdPartOrderId = createOrderResult.OrderId;
            nftOrder.OrderStatus = OrderStatus.UnPay;
            nftOrder.LastModifyTime = DateTime.UtcNow.ToTimestamp().Seconds;
            await AddOrUpdateNFTOrderAsync(nftOrder);
            await _seedLockCache.SetAsync(SeedCachePrefix + input.Symbol, nftOrder.Id.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(OrderConstants.OrderLockSeedExpireTime)
            });
            result.ThirdPartOrderId = nftOrder.ThirdPartOrderId;
            result.OrderId = nftOrder.Id;
        }
        else
        {
            throw new Exception("symbol buying");
        }
        return result;
    }

    private async Task<UserIndex> GetUserIndexAsync()
    {
        var userId = CurrentUser.GetId();
        if (userId == Guid.Empty)
        {
            return null;
        }
        return await _userInformationProvider.GetByIdAsync(userId);
    }

    private string GetChainId(UserIndex index, string address, string defaultChainId = null)
    {
        if (address.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        if (!index.CaAddressListSide.IsNullOrEmpty())
        {
            foreach (var cAddress in index.CaAddressListSide)
            {
                if (cAddress.Address.Equals(address))
                {
                    return cAddress.ChainId;
                }
            }
        }

        return defaultChainId;
    }
    
    private void AssertSeedSymbol(string symbol)
    {
        var words = symbol.Split(NFTSymbolBasicConstants.NFTSymbolSeparator);
        AssertHelper.IsTrue(words[0].Length > 0 && words[0].All(IsValidCreateSymbolChar), "Invalid Symbol input");
        if (words.Length == 1)
        {
            return;
        }
        AssertHelper.IsTrue(words.Length == 2 && words[1] == NFTSymbolBasicConstants.CollectionSymbolSuffix, "Invalid NFT Symbol input");
    }
        
    private bool IsValidCreateSymbolChar(char character)
    {
        return character >= 'A' && character <= 'Z';
    }

    public async Task AddOrUpdateNFTOrderAsync(NFTOrder nftOrder)
    {
        if (nftOrder.Id == Guid.Empty)
        {
            nftOrder.Id = GuidHelper.UniqId(nftOrder.UserId.ToString(), nftOrder.CreateTime.ToString());
        }

        var grain = _clusterClient.GetGrain<INFTOrderGrain>(GrainIdHelper.GenerateGrainId(nftOrder.ChainId, nftOrder.Id));
        var result = await grain.AddOrUpdateAsync(_objectMapper.Map<NFTOrder, NFTOrderGrainDto>(nftOrder));
        if (!result.Success)
        {
            return;
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<NFTOrder, NFTOrderEto>(nftOrder));
    }

    public async Task<NFTOrderDto> SearchOrderAsync(SearchOrderInput input)
    {
        var nftOrderIndex = await _nftOrderIndexRepository.GetAsync(input.OrderId);
        if (nftOrderIndex == null)
        {
            return new NFTOrderDto();
        }

        var result =  _objectMapper.Map<NFTOrderIndex, NFTOrderDto>(nftOrderIndex);
        result.OrderId = nftOrderIndex.Id;
        result.MerchantOrderId = nftOrderIndex.ThirdPartOrderId;
        return result;
    }
}