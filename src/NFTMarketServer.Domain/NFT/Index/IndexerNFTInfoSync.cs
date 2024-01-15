using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTInfoSync : IndexerCommonResult<IndexerNFTInfoSync>
{
    public long TotalRecordCount { get; set; }
    
    public List<NFTInfoIndex> DataList { get; set; }
}