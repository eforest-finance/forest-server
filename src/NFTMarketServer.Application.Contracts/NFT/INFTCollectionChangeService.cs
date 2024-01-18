using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Chain;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT;

public interface INFTCollectionChangeService
{
     Task<long> HandleItemsChangesAsync(string chainId, List<IndexerNFTCollectionChange> collectionChanges);

     Task<long> HandlePriceChangesAsync(string chainId, List<IndexerNFTCollectionPriceChange> collectionChanges,
         long lastEndHeight,
         string businessType);
}