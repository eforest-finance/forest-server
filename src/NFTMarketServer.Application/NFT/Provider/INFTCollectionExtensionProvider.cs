using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Provider;

public interface INFTCollectionExtensionProvider
{
    Task<Dictionary<string,NFTCollectionExtensionIndex>> GetNFTCollectionExtensionsAsync(List<string> nftCollectionExtensionIndexIds);
    
    Task<NFTCollectionExtensionIndex> GetNFTCollectionExtensionAsync(string nftCollectionExtensionIndexId);
    
    Task<Tuple<long, List<NFTCollectionExtensionIndex>>> GetNFTCollectionExtensionAsync(SearchNFTCollectionsInput input);
    
    Task<Tuple<long, List<NFTCollectionExtensionIndex>>> GetNFTCollectionExtensionPageAsync(int skipCount,int limit);
}