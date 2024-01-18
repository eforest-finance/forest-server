using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.SymbolMarketToken.Index;

namespace NFTMarketServer.SymbolMarketToken.Provider;

public interface ISymbolMarketTokenProvider
{
    public Task<IndexerSymbolMarketTokens> GetSymbolMarketTokenAsync(List<string> address, long skipCount,
        long maxResultCount);
    
    public Task<IndexerSymbolMarketIssuer> GetSymbolMarketTokenIssuerAsync(int issueChainId, string tokenSymbol);
    
    public Task<IndexerSymbolMarketTokenExist> GetSymbolMarketTokenExistAsync(string issueChain, string tokenSymbol);
}