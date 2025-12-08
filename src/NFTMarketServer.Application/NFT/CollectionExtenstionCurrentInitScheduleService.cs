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

public class CollectionExtenstionCurrentInitScheduleService : ScheduleSyncDataService, ISingletonDependency
{
    private readonly ILogger<CollectionExtenstionCurrentInitScheduleService> _logger;
    private readonly INFTCollectionExtensionProvider _nftCollectionExtensionProvider;
    private readonly IChainAppService _chainAppService;
    private readonly INFTCollectionChangeService _nftCollectionChangeService;
    
    public CollectionExtenstionCurrentInitScheduleService(ILogger<CollectionExtenstionCurrentInitScheduleService> logger,
        IGraphQLProvider graphQlProvider,
        INFTCollectionExtensionProvider nftCollectionExtensionProvider,
        IChainAppService chainAppService, 
        INFTCollectionChangeService nftCollectionChangeService) : 
        base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _nftCollectionExtensionProvider = nftCollectionExtensionProvider;
        _chainAppService = chainAppService;
        _nftCollectionChangeService = nftCollectionChangeService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId,long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        var limit = 100;
        long maxProcessedBlockHeight = -1;
        var processCollection = new List<NFTCollectionExtensionIndex>();
        //Paging for logical processing
        do
        {
            var collectionPage =
                await _nftCollectionExtensionProvider.GetNFTCollectionExtensionPageAsync(skipCount, limit);

            if (collectionPage == null || collectionPage.Item2.IsNullOrEmpty())
            {
                break;
            }

            var count = collectionPage.Item2.Count;
            _logger.LogInformation("CollectionExtenstionCurrentInitScheduleService queryList chainId:{chainId} count: {count}", 
                chainId, count);

            skipCount += count;

            processCollection = collectionPage.Item2;

            await _nftCollectionChangeService.HandleCurrentInfoInitAsync(processCollection);

        } while (!processCollection.IsNullOrEmpty());

        return maxProcessedBlockHeight;
    }
    
    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(1);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.CollectionExtenstionCurrentInit;
    }
}