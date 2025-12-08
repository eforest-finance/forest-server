using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT;

public class NFTCollectionPriceScheduleService : ScheduleSyncDataService, ITransientDependency
{
    private readonly ILogger<NFTCollectionStatisticalDataScheduleService> _logger;
    private readonly INFTCollectionProvider _nftCollectionProvider;
    private readonly IChainAppService _chainAppService;
    private readonly INFTCollectionChangeService _nftCollectionChangeService;
    
    public NFTCollectionPriceScheduleService(ILogger<NFTCollectionStatisticalDataScheduleService> logger,
        IGraphQLProvider graphQlProvider,
        INFTCollectionProvider nftCollectionProvider,
        IChainAppService chainAppService,
        INFTCollectionChangeService nftCollectionChangeService
    ) : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _nftCollectionProvider = nftCollectionProvider;
        _chainAppService = chainAppService;
        _nftCollectionChangeService = nftCollectionChangeService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        long maxProcessedBlockHeight = -1;
        var processCollectionChanges = new List<IndexerNFTCollectionPriceChange>();
        //Paging for logical processing
        
        var nftCollectionChanges =
            await _nftCollectionProvider.GetNFTCollectionPriceChangesByBlockHeightAsync(skipCount, chainId,
                lastEndHeight);

        if (nftCollectionChanges == null || nftCollectionChanges.IndexerNftCollectionPriceChanges.IsNullOrEmpty())
        {
            return 0;
        }

        var count = nftCollectionChanges.IndexerNftCollectionPriceChanges.Count;
        _logger.LogInformation(
            "GetNFTCollectionPriceChangesByBlockHeightAsync queryList chainId:{chainId} count: {count}",
            chainId, count);

        processCollectionChanges = nftCollectionChanges.IndexerNftCollectionPriceChanges;

        var blockHeight = await _nftCollectionChangeService.HandlePriceChangesAsync(chainId, processCollectionChanges,
            lastEndHeight, GetBusinessType().ToString());

        maxProcessedBlockHeight = Math.Max(maxProcessedBlockHeight, blockHeight);
       

        return maxProcessedBlockHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(1);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.CollectionPrice;
    }
}