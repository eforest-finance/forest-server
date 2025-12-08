using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.SymbolMarketToken.Index;

public class IndexerSymbolMarketTokenExist: IndexerCommonResult<IndexerSymbolMarketTokenExist>
{
    public string Symbol { get; set; }
    public string IssueChain { get; set; }
    public string TokenName { get; set; }
}