using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Options;
using NFTMarketServer.Provider;

namespace NFTMarketServer.NFT;

public class ExpiredListingNftHandleService : ScheduleSyncDataService
{
    private readonly INFTListingProvider _nftListingProvider;
    private readonly IChainAppService _chainAppService;
    private readonly INFTCollectionChangeService _nftCollectionChangeService;
    private readonly IOptionsMonitor<ExpiredNFTSyncOptions> _optionsMonitor;

    public ExpiredListingNftHandleService(ILogger<ScheduleSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        INFTCollectionChangeService nftCollectionChangeService,
        INFTListingProvider nftListingProvider, IOptionsMonitor<ExpiredNFTSyncOptions> optionsMonitor
    ) :
        base(logger, graphQlProvider, chainAppService)
    {
        _chainAppService = chainAppService;
        _nftListingProvider = nftListingProvider;
        _optionsMonitor = optionsMonitor;
        _nftCollectionChangeService = nftCollectionChangeService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var option = _optionsMonitor.CurrentValue;
        var expireTimeGt = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddSeconds(-option.Duration));
        var expiredListingNft = await _nftListingProvider.GetExpiredListingNftAsync(chainId, expireTimeGt);
        //handle task
        return await HandleCollectionPriceAsync(chainId, expiredListingNft);
    }

    private async Task<long> HandleCollectionPriceAsync(string chainId,
        List<IndexerNFTListingInfoResult> expiredListingNft)
    {
        var collectionSymbols = expiredListingNft.Select(info => info.CollectionSymbol).ToHashSet();

        var changes = collectionSymbols
            .Select(collectionSymbol => new IndexerNFTCollectionPriceChange(chainId, collectionSymbol, -1)).ToList();

        return await _nftCollectionChangeService.HandlePriceChangesAsync(chainId, changes);
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        //add multiple chains
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.ExpiredListingNftHandle;
    }
}