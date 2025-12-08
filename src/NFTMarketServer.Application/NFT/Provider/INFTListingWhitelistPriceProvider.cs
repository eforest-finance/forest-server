using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Provider;

public interface INFTListingWhitelistPriceProvider
{
    public Task<List<IndexerListingWhitelistPrice>> GetNFTListingWhitelistPricesAsync(
        string address,
        List<string> nftInfoIds);
}