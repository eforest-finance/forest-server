using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT;

public class NFTCollectionStatisticalDataScheduleService : ScheduleSyncDataService, ISingletonDependency
{
    private readonly ILogger<NFTCollectionStatisticalDataScheduleService> _logger;
    private readonly INFTCollectionProvider _nftCollectionProvider;
    private readonly IChainAppService _chainAppService;
    private readonly INFTCollectionChangeService _nftCollectionChangeService;
    
    public NFTCollectionStatisticalDataScheduleService(ILogger<NFTCollectionStatisticalDataScheduleService> logger,
        IGraphQLProvider graphQlProvider,
        INFTCollectionProvider nftCollectionProvider,
        IChainAppService chainAppService, 
        INFTCollectionChangeService nftCollectionChangeService) : 
        base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _nftCollectionProvider = nftCollectionProvider;
        _chainAppService = chainAppService;
        _nftCollectionChangeService = nftCollectionChangeService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId,long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        long maxProcessedBlockHeight = -1;
        var processCollectionChanges = new List<IndexerNFTCollectionChange>();
        //Paging for logical processing
        do
        {
            var nftCollectionChanges =
                await _nftCollectionProvider.GetNFTCollectionChangesByBlockHeightAsync(skipCount, chainId,
                    lastEndHeight);

            if (nftCollectionChanges == null || nftCollectionChanges.IndexerNftCollectionChanges.IsNullOrEmpty())
            {
                break;
            }

            var count = nftCollectionChanges.IndexerNftCollectionChanges.Count;
            _logger.LogInformation("GetNFTCollectionChangesByBlockHeightAsync queryList list:{list} chainId:{chainId} count: {count} ", 
                JsonConvert.SerializeObject(nftCollectionChanges.IndexerNftCollectionChanges), chainId, count);

            skipCount += count;

            processCollectionChanges = nftCollectionChanges.IndexerNftCollectionChanges;

            var blockHeight = await _nftCollectionChangeService.HandleItemsChangesAsync(chainId, processCollectionChanges);
            
            maxProcessedBlockHeight = Math.Max(maxProcessedBlockHeight, blockHeight);
            
        } while (!processCollectionChanges.IsNullOrEmpty());

        return maxProcessedBlockHeight;
    }
    
    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(1);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.CollectionExtenstion;
    }
}