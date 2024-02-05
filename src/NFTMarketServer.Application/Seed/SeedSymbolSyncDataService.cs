using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Basic;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Provider;
using Orleans.Runtime;
using Volo.Abp.Caching;

namespace NFTMarketServer.Seed;

public class SeedSymbolSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ISeedAppService _seedAppService;
    private readonly IChainAppService _chainAppService;
    private const int HeightExpireMinutes = 5;
    private readonly IDistributedCache<List<string>> _distributedCache;private readonly IDistributedCache<string> _distributedCacheForHeight;

    private readonly IOptionsMonitor<ResetNFTSyncHeightExpireMinutesOptions>
        _resetNFTSyncHeightExpireMinutesOptionsMonitor;


    public SeedSymbolSyncDataService(ILogger<SeedSymbolSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        ISeedAppService seedAppService,
        IDistributedCache<List<string>> distributedCache,
        IDistributedCache<string> distributedCacheForHeight,
        IOptionsMonitor<ResetNFTSyncHeightExpireMinutesOptions> resetNFTSyncHeightExpireMinutesOptionsMonitor,
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _seedAppService = seedAppService;
        _chainAppService = chainAppService;
        _distributedCache = distributedCache;
        _distributedCacheForHeight = distributedCacheForHeight;
        _resetNFTSyncHeightExpireMinutesOptionsMonitor = resetNFTSyncHeightExpireMinutesOptionsMonitor;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        try
        {
            var resetSyncHeightFlag = await _distributedCache.GetAsync(CommonConstant.ResetNFTSyncHeightFlagCacheKey);
            var seedSyncHeightFlag = await _distributedCache.GetAsync(CommonConstant.SeedResetHeightFlagCacheKey);
            _logger.Debug("GetCompositeNFTInfosAsync seed {ResetSyncHeightFlag} {SeedSyncHeightFlag} {resetNftSyncHeightExpireMinutes}",
                resetSyncHeightFlag, seedSyncHeightFlag,
                _resetNFTSyncHeightExpireMinutesOptionsMonitor?.CurrentValue.ResetNFTSyncHeightExpireMinutes);
            if (!resetSyncHeightFlag.IsNullOrEmpty())
            {
                if (seedSyncHeightFlag.IsNullOrEmpty())
                {
                    var resetNftSyncHeightExpireMinutes =
                        _resetNFTSyncHeightExpireMinutesOptionsMonitor?.CurrentValue.ResetNFTSyncHeightExpireMinutes ??
                        CommonConstant.CacheExpirationMinutes;

                    await _distributedCacheForHeight.SetAsync(CommonConstant.SeedResetHeightFlagCacheKey,
                        CommonConstant.SeedResetHeightFlagCacheKey, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(resetNftSyncHeightExpireMinutes)
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