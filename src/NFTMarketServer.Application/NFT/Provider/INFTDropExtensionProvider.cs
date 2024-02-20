using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Provider;

public interface INFTDropExtensionProvider
{ 
    Task<Dictionary<string, NFTDropExtensionIndex>> BatchGetNFTDropExtensionAsync(List<string> dropIds);
}