using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Options;
using NFTMarketServer.Provider;
using Serilog;

namespace NFTMarketServer.NFT;

public class ExpiredListingNftHandleService : ScheduleSyncDataService
{
    private readonly INFTListingProvider _nftListingProvider;
    private readonly IChainAppService _chainAppService;
    private readonly INFTCollectionChangeService _nftCollectionChangeService;
    private readonly IOptionsMonitor<ExpiredNFTSyncOptions> _optionsMonitor;
    private readonly IBus _bus;

    public ExpiredListingNftHandleService(ILogger<ScheduleSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        INFTCollectionChangeService nftCollectionChangeService,
        IBus bus,
        INFTListingProvider nftListingProvider, IOptionsMonitor<ExpiredNFTSyncOptions> optionsMonitor
    ) :
        base(logger, graphQlProvider, chainAppService)
    {
        _chainAppService = chainAppService;
        _nftListingProvider = nftListingProvider;
        _optionsMonitor = optionsMonitor;
        _nftCollectionChangeService = nftCollectionChangeService;
        _bus = bus;
    }
    
    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var option = _optionsMonitor.CurrentValue;
        var expireTimeGt = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddSeconds(-option.Duration));
        var expiredListingNft = await _nftListingProvider.GetExpiredListingNftAsync(chainId, expireTimeGt);
        if (expiredListingNft == null || expiredListingNft.IsNullOrEmpty())
        {
            return 0;
        }
        //handle task
        return await HandleCollectionPriceAsync(chainId, expiredListingNft, lastEndHeight);
    }

    private async Task<long> HandleCollectionPriceAsync(string chainId,
        List<IndexerNFTListingInfoResult> expiredListingNft, long lastEndHeight)
    {
        
        var distinctListings = expiredListingNft
            .GroupBy(nft => nft.NftInfoId)
            .Select(group => group.First())
            .ToList();
        foreach (var item in distinctListings)
        {
            await _bus.Publish(new NewIndexEvent<NFTListingChangeEto>
            {
                Data = new NFTListingChangeEto
                {
                    Symbol = item.Symbol,
                    ChainId = chainId,
                    NftId = item.NftInfoId
                }
            });
        }
        
        var collectionSymbols = expiredListingNft.Select(info => info.CollectionSymbol).ToHashSet();

        var changes = collectionSymbols
            .Select(collectionSymbol => new IndexerNFTCollectionPriceChange(chainId, collectionSymbol, -1)).ToList();

        return await _nftCollectionChangeService.HandlePriceChangesAsync(chainId, changes, lastEndHeight,
            GetBusinessType().ToString());
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