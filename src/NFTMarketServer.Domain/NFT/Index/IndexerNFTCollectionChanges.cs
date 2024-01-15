using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTCollectionChanges : IndexerCommonResult<IndexerNFTCollectionChanges>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerNFTCollectionChange> IndexerNftCollectionChanges { get; set; }
}

public class IndexerNFTCollectionChange : IndexerCommonResult<IndexerNFTCollectionChange>
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public long BlockHeight { get; set; }
    
    public IndexerNFTCollectionChange(string chainId, string symbol, long blockHeight)
    {
        ChainId = chainId;
        Symbol = symbol;
        BlockHeight = blockHeight;
    }
}
