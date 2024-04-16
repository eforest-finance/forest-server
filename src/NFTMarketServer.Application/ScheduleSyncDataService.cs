using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
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


    public async Task DealDataAsync(bool resetHeightFlag, long resetHeight)
    {
        var businessQueryChainType = GetBusinessType();
        var chainIds = await GetChainIdsAsync();
        //handle multiple chains
        foreach (var chainId in chainIds)
        {
            try
            {
                if (resetHeightFlag)
                {
                    await _graphQlProvider.SetLastEndHeightAsync(chainId, businessQueryChainType, resetHeight);
                    _logger.LogInformation(
                        "Reset blockHeight for businessType: {businessQueryChainType} chainId: {chainId} lastEndHeight: {BlockHeight}",
                        businessQueryChainType, chainId, resetHeight);
                    return;
                }
                
                var lastEndHeight = await _graphQlProvider.GetLastEndHeightAsync(chainId, businessQueryChainType);
                var newIndexHeight = await _graphQlProvider.GetIndexBlockHeightAsync(chainId);
                _logger.LogInformation(
                    "Start deal data for businessType: {businessQueryChainType} chainId: {chainId} lastEndHeight: {lastEndHeight} newIndexHeight: {newIndexHeight}",
                    businessQueryChainType, chainId, lastEndHeight, newIndexHeight);
                var blockHeight = await SyncIndexerRecordsAsync(chainId, lastEndHeight, newIndexHeight);

                if (blockHeight > 0)
                {
                    await _graphQlProvider.SetLastEndHeightAsync(chainId, businessQueryChainType, blockHeight);
                    _logger.LogInformation(
                        "End deal data for businessType: {businessQueryChainType} chainId: {chainId} lastEndHeight: {BlockHeight}",
                        businessQueryChainType, chainId, blockHeight);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "DealDataAsync error businessQueryChainType:{businessQueryChainType} chainId: {chainId}",
                    businessQueryChainType.ToString(), chainId);
            }
        }
    }

    public abstract Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight);

    /**
     * different businesses obtain different multiple chains
     */
    public abstract Task<List<string>> GetChainIdsAsync();

    public abstract BusinessQueryChainType GetBusinessType();
}