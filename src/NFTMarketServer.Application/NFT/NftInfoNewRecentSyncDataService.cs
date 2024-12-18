using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Basic;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using Orleans.Runtime;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.NFT;

public class NftInfoNewRecentSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly IChainAppService _chainAppService;
    private const int HeightExpireMinutes = 5;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private readonly IDistributedEventBus _distributedEventBus;

    public NftInfoNewRecentSyncDataService(ILogger<NftInfoNewRecentSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        INFTInfoAppService nftInfoAppService,
        IDistributedCache<List<string>> distributedCache,
        IDistributedEventBus distributedEventBus, 
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _nftInfoAppService = nftInfoAppService;
        _chainAppService = chainAppService;
        _distributedCache = distributedCache;
        _distributedEventBus = distributedEventBus;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        if (newIndexHeight - CommonConstant.RecentSearchNumber > lastEndHeight)
        {
            lastEndHeight = newIndexHeight - CommonConstant.RecentSearchNumber;
        }
        var queryList = await _graphQlProvider.GetSyncNftInfoRecordsAsync(chainId, lastEndHeight, 0);
        _logger.LogInformation(
            "SyncNftInfoNewRecentRecords queryList startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
            lastEndHeight, newIndexHeight, queryList?.Count);
        long blockHeight = -1;
        if (queryList.IsNullOrEmpty())
        {
            return 0;
        }

        var cacheKey = GetBusinessType() + chainId + lastEndHeight;
        List<string> symbolList = await _distributedCache.GetAsync(cacheKey);
        foreach (var nftInfo in queryList)
        {
            if (nftInfo == null) continue;
            var innerKey = nftInfo.Symbol + nftInfo.BlockHeight;
            if (symbolList != null && symbolList.Contains(innerKey))
            {
                _logger.LogDebug("GetSyncNftInfoNewRecentRecordsAsync duplicated symbol: {symbol}", nftInfo.Symbol);
                continue;
            }
            
            _logger.LogDebug("GetSyncNftInfoNewRecentRecordsAsync NFTInfoSymbol {NFTInfoSymbol}",nftInfo.Symbol);
            

            blockHeight = Math.Max(blockHeight, nftInfo.BlockHeight);
            await _nftInfoAppService.AddOrUpdateNftInfoNewAsync(nftInfo, nftInfo.Id, chainId);

            if (chainId.Equals(CommonConstant.MainChainId))
            {
                continue;
            }
            var collectionId = nftInfo.CollectionId;
            var utcHourStartTimestamp = TimeHelper.GetUtcHourStartTimestamp();
            var utcHourStart = TimeHelper.FromUnixTimestampSeconds(utcHourStartTimestamp);
            var utcHourStartStr = TimeHelper.GetDateTimeFormatted(utcHourStart);
            await _distributedEventBus.PublishAsync(new NFTCollectionTradeEto
            {
                Id = IdGenerateHelper.GetHourlyCollectionTradeRecordId(collectionId, utcHourStartStr),
                CollectionId = collectionId,
                ChainId = chainId,
                CurrentOrdinal = utcHourStartTimestamp,
                CurrentOrdinalStr = utcHourStartStr
            });
        }

        if (blockHeight > 0)
        {
            symbolList = queryList.Where(obj => obj.BlockHeight == blockHeight)
                .Select(obj => obj.Symbol + obj.BlockHeight)
                .ToList();
            await _distributedCache.SetAsync(cacheKey, symbolList,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(HeightExpireMinutes)
                });
        }

        return blockHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(CommonConstant.IntOne);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.NftInfoNewRecentSync;
    }
}