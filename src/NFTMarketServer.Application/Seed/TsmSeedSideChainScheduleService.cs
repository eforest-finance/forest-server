using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Provider;

namespace NFTMarketServer.Seed;

public class TsmSeedSideChainScheduleService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ISeedAppService _seedAppService;
    private readonly IChainAppService _chainAppService;
    
    public TsmSeedSideChainScheduleService(ILogger<TsmSeedSideChainScheduleService> logger, IGraphQLProvider graphQlProvider,
        ISeedAppService seedAppService, IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _seedAppService = seedAppService;
        _chainAppService = chainAppService;
    }
    
    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var queryList = await _graphQlProvider.GetSyncTsmSeedRecordsAsync(chainId, lastEndHeight, 0);
        _logger.LogInformation(
            "SyncTsmSeedSideChainRecordsAsync queryList startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
            lastEndHeight, newIndexHeight, queryList.Count);
        long blockHeight = -1;
        if (queryList.IsNullOrEmpty())
        {
            return 0;
        }

        foreach (var queryDto in queryList)
        {
            blockHeight = Math.Max(blockHeight, queryDto.BlockHeight);
            await _seedAppService.AddOrUpdateTsmSeedInfoAsync(queryDto);
        }
        
        return blockHeight;
    }
    
    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(1);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.TsmSeedSymbolSideChain;
    }
}