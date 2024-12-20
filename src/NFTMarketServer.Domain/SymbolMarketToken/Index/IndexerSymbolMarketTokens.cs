using System.Collections.Generic;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.SymbolMarketToken.Index;

public class IndexerSymbolMarketTokens : IndexerCommonResult<IndexerSymbolMarketTokens>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerSymbolMarketToken> IndexerSymbolMarketTokenList { get; set; }
}

public class IndexerSymbolMarketToken : IndexerCommonResult<IndexerSymbolMarketToken>
{
    public string SymbolMarketTokenLogoImage { get; set; }
    public string Symbol { get; set; }
    
    public string TokenName { get; set; }
    
    public string Issuer { get; set; }
    public string Owner { get; set; }

    public long IssueChainId { get; set; }
    public int Decimals { get; set; }
    public long TotalSupply { get; set; }
    public long Supply { get; set; }
    public long Issued { get; set; }
    public List<string> IssueManagerList { get; set; }
}