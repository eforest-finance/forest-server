using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AElf.Indexing.Elasticsearch;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using NFTMarketServer.Basic;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.Provider;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Seed;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Options;
using Orleans.Runtime;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT;

public class ExpiredNftMinPriceSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly IChainAppService _chainAppService;
    private readonly INFTListingProvider _nftListingProvider;
    private readonly ISeedAppService _seedAppService;
    private readonly INESTRepository<NFTInfoIndex, string> _nftInfoIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly IOptionsMonitor<ExpiredNFTSyncOptions> _optionsMonitor;
    private readonly IBus _bus;
    private readonly IObjectMapper _objectMapper;
    private const int HeightExpireMinutes = 10;
    private readonly IDistributedCache<List<string>> _distributedCache;
    
    public ExpiredNftMinPriceSyncDataService(ILogger<NftInfoSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        INFTInfoAppService nftInfoAppService, 
        IChainAppService chainAppService,
        INFTListingProvider nftListingProvider,
        ISeedAppService seedAppService,
        IBus bus,
        IObjectMapper objectMapper,
        INESTRepository<NFTInfoIndex, string> nftInfoIndexRepository, 
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        IDistributedCache<List<string>> distributedCache,
        IOptionsMonitor<ExpiredNFTSyncOptions> optionsMonitor)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _nftInfoAppService = nftInfoAppService;
        _chainAppService = chainAppService;
        _nftListingProvider = nftListingProvider;
        _seedAppService = seedAppService;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _optionsMonitor = optionsMonitor;
        _bus = bus;
        _objectMapper = objectMapper;
        _distributedCache = distributedCache;
    }
    
    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var option = _optionsMonitor.CurrentValue;
        var list = await _nftListingProvider.GetNftMinPriceAsync(chainId, option.Duration);
        _logger.Debug("GetMinPriceNft, duration: {duration}", option.Duration);
        long blockHeight = -1;
        if (list.IsNullOrEmpty())
        {
            return 0;
        }

        var cacheKey = GetBusinessType() + chainId + lastEndHeight;
        List<string> nftInfoIdWithTimeList = await _distributedCache.GetAsync(cacheKey);
        List<string> nftInfoIdWithTimeListNew = new List<string>();
        foreach (var data in list)
        {
            var nftInfoIdWithTime = data.Key+ DateTimeHelper.ToUnixTimeMilliseconds(data.Value.ExpireTime);
            nftInfoIdWithTimeListNew.Add(nftInfoIdWithTime);

            if (nftInfoIdWithTimeList != null && nftInfoIdWithTimeList.Contains(nftInfoIdWithTime))
            {
                _logger.Debug($"ExpiredNftMinPriceSync duplicated nftInfoIdWithTime: {nftInfoIdWithTime}", nftInfoIdWithTime);
                continue;
            }
            var nftInfoId = data.Key;
            var isSeed = nftInfoId.Match(NFTSymbolBasicConstants.SeedIdPattern);
            if (isSeed)
            {
                var seedSymbol = await _seedSymbolIndexRepository.GetAsync(nftInfoId);
                if (seedSymbol == null) continue;
                var minNftListing = data.Value;
                seedSymbol.HasListingFlag = minNftListing != null;
                seedSymbol.MinListingPrice = minNftListing?.Prices ?? 0;
                seedSymbol.MinListingExpireTime = minNftListing?.ExpireTime;
                seedSymbol.MinListingId = minNftListing?.Id;
                
                await _seedAppService.AddOrUpdateSeedSymbolAsync(seedSymbol);
            }
            else
            {
                var nftInfo = await _nftInfoIndexRepository.GetAsync(nftInfoId);
                if (nftInfo == null) continue;
                var minNftListing = data.Value;
                nftInfo.HasListingFlag = minNftListing != null;
                nftInfo.MinListingPrice = minNftListing?.Prices ?? 0;
                nftInfo.MinListingExpireTime = minNftListing?.ExpireTime;
                nftInfo.MinListingId = minNftListing?.Id;
                
                await _nftInfoAppService.AddOrUpdateNftInfoAsync(nftInfo);
            }
            await _bus.Publish(new NewIndexEvent<NFTListingChangeEto>
            {
                Data = new NFTListingChangeEto
                {
                    Symbol = data.Value.Symbol,
                    ChainId = chainId,
                    NftId = IdGenerateHelper.GetNFTInfoId(chainId,data.Value.Symbol)
                }
            });
            await _bus.Publish<NewIndexEvent<NFTOfferChangeDto>>(new NewIndexEvent<NFTOfferChangeDto>
            {
                Data = new NFTOfferChangeDto
                {
                    NftId = IdGenerateHelper.GetNFTInfoId(chainId, data.Value.Symbol),
                    ChainId = chainId
                }
            });
        }
        
        await _distributedCache.SetAsync(cacheKey, nftInfoIdWithTimeListNew,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(HeightExpireMinutes)
            });
        
        return blockHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        //add multiple chains
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.ExpiredNftMinPriceSync;
    }
}