using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTCollectionPriceChanges : IndexerCommonResult<IndexerNFTCollectionPriceChanges>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerNFTCollectionPriceChange> IndexerNftCollectionPriceChanges { get; set; }
}

public class IndexerNFTCollectionPriceChange : IndexerCommonResult<IndexerNFTCollectionPriceChange>
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public long BlockHeight { get; set; }

    public IndexerNFTCollectionPriceChange(string chainId, string symbol, long blockHeight)
    {
        ChainId = chainId;
        Symbol = symbol;
        BlockHeight = blockHeight;
    }
}
