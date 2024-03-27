using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Options;
using NFTMarketServer.Provider;
using Orleans.Runtime;
using Serilog;
using Volo.Abp.Caching;

namespace NFTMarketServer.NFT;

public class ExpiredListingNftHandleService : ScheduleSyncDataService
{
    private readonly INFTListingProvider _nftListingProvider;
    private readonly IChainAppService _chainAppService;
    private readonly INFTCollectionChangeService _nftCollectionChangeService;
    private readonly IOptionsMonitor<ExpiredNFTSyncOptions> _optionsMonitor;
    private readonly IBus _bus;
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private const int HeightExpireMinutes = 10;

    public ExpiredListingNftHandleService(ILogger<ScheduleSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        INFTCollectionChangeService nftCollectionChangeService,
        IBus bus,
        IDistributedCache<List<string>> distributedCache,
        INFTListingProvider nftListingProvider, IOptionsMonitor<ExpiredNFTSyncOptions> optionsMonitor
    ) :
        base(logger, graphQlProvider, chainAppService)
    {
        _chainAppService = chainAppService;
        _nftListingProvider = nftListingProvider;
        _optionsMonitor = optionsMonitor;
        _nftCollectionChangeService = nftCollectionChangeService;
        _bus = bus;
        _logger = logger;
        _distributedCache = distributedCache;
    }
    
    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var option = _optionsMonitor.CurrentValue;
        var expireTimeGt = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddSeconds(-option.Duration));
        var expiredListingNft = await _nftListingProvider.GetExpiredListingNftAsync(chainId, expireTimeGt);
        if (expiredListingNft == null || expiredListingNft.IsNullOrEmpty())
        {
            _logger.LogInformation(
                "GetExpiredListingNftAsync no data");
            return 0;
        }
        
        //handle task
        return await HandleCollectionPriceAsync(chainId, expiredListingNft, lastEndHeight);
    }

    private async Task<long> HandleCollectionPriceAsync(string chainId,
        List<IndexerNFTListingInfoResult> expiredListingNft, long lastEndHeight)
    {
        
        var distinctListings = expiredListingNft
            .GroupBy(nft => nft.NftInfoId)
            .Select(group => group.First())
            .ToList();
        var cacheKey = GetBusinessType() + chainId + lastEndHeight;
        var nftInfoIdWithTimeList = await _distributedCache.GetAsync(cacheKey);
        var nftInfoIdWithTimeListNew = new List<string>();
        var changeFlag = false;
        foreach (var data in distinctListings)
        {
            if (data == null || data.NftInfoId.IsNullOrEmpty() || data?.ExpireTime == null)
            {
                _logger.Debug("ExpiredListingForCollectionUpdate null check {Data}", JsonConvert.SerializeObject(data));
                continue;
            }
            
            var nftInfoIdWithTime = data.NftInfoId + DateTimeHelper.ToUnixTimeMilliseconds(data.ExpireTime);
            nftInfoIdWithTimeListNew.Add(nftInfoIdWithTime);

            if (!nftInfoIdWithTimeList.IsNullOrEmpty() && nftInfoIdWithTimeList.Contains(nftInfoIdWithTime))
            {
                _logger.Debug($"ExpiredListingForCollectionUpdate duplicated nftInfoIdWithTime: {nftInfoIdWithTime}",
                    nftInfoIdWithTime);
                continue;
            }

            changeFlag = true;
            await _bus.Publish(new NewIndexEvent<NFTListingChangeEto>
            {
                Data = new NFTListingChangeEto
                {
                    Symbol = data.Symbol,
                    ChainId = chainId,
                    NftId = data.NftInfoId
                }
            });
        }
        
        await _distributedCache.SetAsync(cacheKey, nftInfoIdWithTimeListNew,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(HeightExpireMinutes)
            });
        
        if (!changeFlag)
        {
            return -1;
        }
        var collectionSymbols = expiredListingNft.Select(info => info.CollectionSymbol).ToHashSet();

        var changes = collectionSymbols
            .Select(collectionSymbol => new IndexerNFTCollectionPriceChange(chainId, collectionSymbol, -1)).ToList();

        return await _nftCollectionChangeService.HandlePriceChangesAsync(chainId, changes, lastEndHeight,
            GetBusinessType().ToString());
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        //add multiple chains
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.ExpiredListingNftHandle;
    }
}