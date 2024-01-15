using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Provider;

namespace NFTMarketServer.NFT;

public class NftInfoSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly IChainAppService _chainAppService;

    public NftInfoSyncDataService(ILogger<NftInfoSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        INFTInfoAppService nftInfoAppService, 
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _nftInfoAppService = nftInfoAppService;
        _chainAppService = chainAppService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var queryList = await _graphQlProvider.GetSyncNftInfoRecordsAsync(chainId, lastEndHeight, 0);
        _logger.LogInformation(
            "SyncNftInfoRecords queryList startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
            lastEndHeight, newIndexHeight, queryList?.Count);
        long blockHeight = -1;
        if (queryList.IsNullOrEmpty())
        {
            return 0;
        }

        foreach (var nftInfo in queryList)
        {
            blockHeight = Math.Max(blockHeight, nftInfo.BlockHeight);
            await _nftInfoAppService.AddOrUpdateNftInfoAsync(nftInfo);
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
        return BusinessQueryChainType.NftInfoSync;
    }
}