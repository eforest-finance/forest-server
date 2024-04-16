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
using NFTMarketServer.Provider;
using Orleans.Runtime;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.Seed;

public class SeedSymbolSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ISeedAppService _seedAppService;
    private readonly IChainAppService _chainAppService;
    private const int HeightExpireMinutes = 5;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private readonly IDistributedCache<string> _distributedCacheForHeight;
    private readonly IDistributedEventBus _distributedEventBus;
    
    public SeedSymbolSyncDataService(ILogger<SeedSymbolSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        ISeedAppService seedAppService,
        IDistributedCache<List<string>> distributedCache,
        IDistributedCache<string> distributedCacheForHeight,
        IDistributedEventBus distributedEventBus,
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _seedAppService = seedAppService;
        _chainAppService = chainAppService;
        _distributedCache = distributedCache;
        _distributedCacheForHeight = distributedCacheForHeight;
        _distributedEventBus = distributedEventBus;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        try
        {
            var resetSyncHeightFlagMinutesStr = await _distributedCacheForHeight.GetAsync(CommonConstant.ResetNFTSyncHeightFlagCacheKey);
            var seedResetHeightFlagCacheValue = await _distributedCacheForHeight.GetAsync(CommonConstant.SeedResetHeightFlagCacheKey+chainId);
            _logger.Debug("GetCompositeNFTInfosAsync seed {ResetSyncHeightFlag} {SeedSyncHeightFlag} {SeedSyncHeightFlagMinuteStr}",
                resetSyncHeightFlagMinutesStr, seedResetHeightFlagCacheValue,
                resetSyncHeightFlagMinutesStr);
            if (!resetSyncHeightFlagMinutesStr.IsNullOrEmpty())
            {
                if (seedResetHeightFlagCacheValue.IsNullOrEmpty())
                {
                    var resetSeedSyncHeightExpireMinutes =
                        int.Parse(resetSyncHeightFlagMinutesStr);

                    await _distributedCacheForHeight.SetAsync(CommonConstant.SeedResetHeightFlagCacheKey+chainId,
                        CommonConstant.SeedResetHeightFlagCacheKey, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(resetSeedSyncHeightExpireMinutes)
                        });

                    return CommonConstant.BeginHeight;
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(CommonConstant.IntError, "Something is wrong for SyncIndexerRecordsAsync reset height", e);
        }
        
        var queryList = await _graphQlProvider.GetSyncSeedSymbolRecordsAsync(chainId, lastEndHeight, 0);
        _logger.LogInformation(
            "SyncSeedSymbolRecords queryList startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
            lastEndHeight, newIndexHeight, queryList?.Count);
        long blockHeight = -1;
        if (queryList.IsNullOrEmpty())
        {
            return 0;
        }

        var cacheKey = GetBusinessType() + chainId + lastEndHeight;
        List<string> symbolList = await _distributedCache.GetAsync(cacheKey);
        foreach (var seedSymbol in queryList)
        {
            var innerKey = seedSymbol.Symbol + seedSymbol.BlockHeight;
            if (symbolList != null && symbolList.Contains(innerKey))
            {
                _logger.Debug("GetSyncSeedSymbolRecordsAsync duplicated symbol: {symbol}", seedSymbol.Symbol);
                continue;
            }
            blockHeight = Math.Max(blockHeight, seedSymbol.BlockHeight);
            await _seedAppService.AddOrUpdateSeedSymbolAsync(seedSymbol);
            if (chainId.Equals(CommonConstant.MainChainId))
            {
                continue;
            }
            var collectionId = NFTSymbolBasicConstants.SeedCollectionSymbol;
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
        //add multiple chains
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.SeedSymbolSync;
    }
}