using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Provider;

public interface ISeedOwnedSymboProvider
{
    public Task<IndexerSeedOwnedSymbols> GetSeedOwnedSymbolsIndexAsync(long inputSkipCount,
        long inputMaxResultCount, string address, string seedOwnedSymbol);
}