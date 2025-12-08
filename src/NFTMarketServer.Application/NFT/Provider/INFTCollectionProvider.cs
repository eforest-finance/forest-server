using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Provider;

public interface INFTCollectionProvider
{
    public Task<IndexerNFTCollections> GetNFTCollectionsIndexAsync(long skipCount, long maxResultCount,
        List<string> addressList);

    public Task<IndexerNFTCollection> GetNFTCollectionIndexAsync(string inputId);
    public Task<Dictionary<string, IndexerNFTCollection>> GetNFTCollectionIndexByIdsAsync(List<string> inputIds);

    Task<IndexerNFTCollectionChanges> GetNFTCollectionChangesByBlockHeightAsync(int skipCount, string chainId, long startBlockHeight);
    
    Task<IndexerNFTCollectionPriceChanges> GetNFTCollectionPriceChangesByBlockHeightAsync(int skipCount, string chainId, long startBlockHeight);
    
    Task<IndexerNFTCollectionExtension> GenerateNFTCollectionExtensionById(string chainId, string symbol);
    
    Task<IndexerNFTCollectionPrice> GetNFTCollectionPriceAsync(string chainId, string symbol, decimal floorPrice);

    Task<IndexerNFTCollectionTrade> GetNFTCollectionTradeAsync(string chainId, string collectionId,
        long beginUtcStamp, long endUtcStamp);
}