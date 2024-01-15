using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Provider;

public interface INFTInfoExtensionProvider
{
    Task<Dictionary<string,NFTInfoExtensionIndex>> GetNFTInfoExtensionsAsync(List<string> nftInfoExtensionIndexIds);
}