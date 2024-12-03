using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Contracts.HandleException;
using NFTMarketServer.Provider;

namespace NFTMarketServer;

public abstract class ScheduleSyncDataService : IScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IChainAppService _chainAppService;

    protected ScheduleSyncDataService(ILogger<ScheduleSyncDataService> logger, IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _chainAppService = chainAppService;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ScheduleSyncDataService.DealDataAsync error msg:",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new[] { "chainId", "resetHeightFlag", "resetHeight", "businessQueryChainType" }
    )]
    public virtual async Task DealDataSingleChainAsync(string chainId, bool resetHeightFlag, long resetHeight,
        BusinessQueryChainType businessQueryChainType)
    {
        if (resetHeightFlag && businessQueryChainType != BusinessQueryChainType.InscriptionCrossChain)
        {
            await _graphQlProvider.SetLastEndHeightAsync(chainId, businessQueryChainType, resetHeight);
            _logger.LogInformation(
                "Reset blockHeight for businessType: {businessQueryChainType} chainId: {chainId} lastEndHeight: {BlockHeight}",
                businessQueryChainType, chainId, resetHeight);
            return;
        }

        var lastEndHeight = await _graphQlProvider.GetLastEndHeightAsync(chainId, businessQueryChainType);
        var newIndexHeight = await _graphQlProvider.GetIndexBlockHeightAsync(chainId, businessQueryChainType);
        _logger.LogInformation(
            "Start deal data for businessType: {businessQueryChainType} chainId: {chainId} lastEndHeight: {lastEndHeight} newIndexHeight: {newIndexHeight}",
            businessQueryChainType, chainId, lastEndHeight, newIndexHeight);
        var preLastEndHeight = lastEndHeight;
        var blockHeight = await SyncIndexerRecordsAsync(chainId, lastEndHeight, newIndexHeight);
        if (blockHeight > 0 && preLastEndHeight == blockHeight && lastEndHeight < newIndexHeight)
        {
            var realBlockHeight = blockHeight;
            blockHeight = Math.Max(realBlockHeight, preLastEndHeight) + 1;
            _logger.LogInformation(
                "blockHeight keep same then change for businessType: {businessQueryChainType} chainId: {chainId} " +
                "preLastEndHeight:{A} realBlockHeight: {B} BlockHeight:{C}",
                businessQueryChainType, chainId, preLastEndHeight, realBlockHeight, blockHeight);
        }

        if (blockHeight > 0)
        {
            await _graphQlProvider.SetLastEndHeightAsync(chainId, businessQueryChainType, blockHeight);
            _logger.LogInformation(
                "End deal data for businessType: {businessQueryChainType} chainId: {chainId} lastEndHeight: {BlockHeight}",
                businessQueryChainType, chainId, blockHeight);
        }
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ScheduleSyncDataService.DealDataAsync error",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new[] { "resetHeightFlag", "resetHeight" }
    )]
    public virtual async Task DealDataAsync(bool resetHeightFlag, long resetHeight)
    {
        var businessQueryChainType = GetBusinessType();
        var chainIds = await GetChainIdsAsync();
        //handle multiple chains
        foreach (var chainId in chainIds)
        {
            await DealDataSingleChainAsync(chainId, resetHeightFlag, resetHeight, businessQueryChainType);
        }
    }

    public abstract Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight);

    /**
     * different businesses obtain different multiple chains
     */
    public abstract Task<List<string>> GetChainIdsAsync();

    public abstract BusinessQueryChainType GetBusinessType();
}