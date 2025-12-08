using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTInfoMarketDatas : IndexerCommonResult<IndexerNFTInfoMarketDatas>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerNFTInfoMarketData> indexerNftInfoMarketDatas { get; set; }
}

public class IndexerNFTInfoMarketData : IndexerCommonResult<IndexerNFTInfoMarketData>
{
    public decimal Price { get; set; }

    public long Timestamp { get; set; }
}