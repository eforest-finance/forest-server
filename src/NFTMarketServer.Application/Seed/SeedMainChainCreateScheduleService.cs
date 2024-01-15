using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using NFTMarketServer.Synchronize;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Provider;

namespace NFTMarketServer.Seed;

public class SeedMainChainCreateScheduleService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IChainAppService _chainAppService;

    private readonly IOptionsMonitor<SynchronizeSeedJobOptions> _seedOptionsMonitor;
    private readonly ISeedInfoProvider _seedInfoProvider;
    private readonly ISynchronizeAppService _synchronizeAppService;

    public SeedMainChainCreateScheduleService(ILogger<ScheduleSyncDataService> logger,
        ISeedInfoProvider seedInfoProvider, IGraphQLProvider graphQlProvider,
        ISynchronizeAppService synchronizeAppService,
        IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        IOptionsMonitor<SynchronizeSeedJobOptions> seedOptionsMonitor,
        IChainAppService chainAppService) : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _seedOptionsMonitor = seedOptionsMonitor;
        _graphQlProvider = graphQlProvider;
        _chainAppService = chainAppService;
        _seedInfoProvider = seedInfoProvider;
        _synchronizeAppService = synchronizeAppService;
    }


    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        long maxProcessedBlockHeight = -1;
        var processChangeList = new List<IndexerSeedMainChainChange>();
        //Paging for logical processing
        do
        {
            var changePageInfo = await _seedInfoProvider.GetIndexerSeedMainChainChangePageByBlockHeightAsync(skipCount, chainId,
                    lastEndHeight);

            if (changePageInfo == null || changePageInfo.IndexerSeedMainChainChangeList.IsNullOrEmpty())
            {
                break;
            }

            var count = changePageInfo.IndexerSeedMainChainChangeList.Count;
            _logger.LogInformation(
                "GetIndexerSeedMainChainChangePageByBlockHeightAsync queryList chainId:{chainId} count: {count}",
                chainId, count);

            skipCount += count;

            processChangeList = changePageInfo.IndexerSeedMainChainChangeList;

            var blockHeight = await HandleSeedMainChainCreateAsync(chainId, processChangeList);

            maxProcessedBlockHeight = Math.Max(maxProcessedBlockHeight, blockHeight);
        } while (!processChangeList.IsNullOrEmpty());

        return maxProcessedBlockHeight;
    }
    
    private async Task<long> HandleSeedMainChainCreateAsync(string chainId,
        List<IndexerSeedMainChainChange> seedChangeList)
    {
        long blockHeight = -1;
        var stopwatch = new Stopwatch();
        foreach (var seedChange in seedChangeList)
        {
            //mark maxProcessedBlockHeight
            blockHeight = Math.Max(blockHeight, seedChange.BlockHeight);
            stopwatch.Start();
            await CrossChainCreateSeedAsync(seedChange);
            stopwatch.Stop();
            _logger.LogInformation(
                "It took {Elapsed} ms to execute CrossChainCreateSeedAsync for seed symbol ChainId:{chainId} seed symbol: {symbol} transactionId: {data}.",
                stopwatch.ElapsedMilliseconds, chainId, seedChange.Symbol, seedChange.TransactionId);

        }

        return blockHeight;
    }

    private async Task CrossChainCreateSeedAsync(IndexerSeedMainChainChange change)
    {
        change.ToChainId = _seedOptionsMonitor.CurrentValue.ToChainId;
        await _synchronizeAppService.SendSeedMainChainCreateSyncAsync(change);
    }
    
    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(0);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.SeedMainChainCreateSync;
    }
}