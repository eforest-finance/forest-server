using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Provider;

namespace NFTMarketServer.Seed;

public class SeedSymbolSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ISeedAppService _seedAppService;
    private readonly IChainAppService _chainAppService;

    public SeedSymbolSyncDataService(ILogger<SeedSymbolSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        ISeedAppService seedAppService, 
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _seedAppService = seedAppService;
        _chainAppService = chainAppService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var queryList = await _graphQlProvider.GetSyncSeedSymbolRecordsAsync(chainId, lastEndHeight, 0);
        _logger.LogInformation(
            "SyncSeedSymbolRecords queryList startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
            lastEndHeight, newIndexHeight, queryList?.Count);
        long blockHeight = -1;
        if (queryList.IsNullOrEmpty())
        {
            return 0;
        }

        foreach (var seedSymbol in queryList)
        {
            blockHeight = Math.Max(blockHeight, seedSymbol.BlockHeight);
            await _seedAppService.AddOrUpdateSeedSymbolAsync(seedSymbol);
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