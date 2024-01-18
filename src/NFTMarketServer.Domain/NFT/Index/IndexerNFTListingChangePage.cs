using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTListingChangePage : IndexerCommonResult<IndexerNFTListingChangePage>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerNFTListingChange> IndexerNFTListingChangeList { get; set; }
}

public class IndexerNFTListingChange : IndexerCommonResult<IndexerNFTListingChange>
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public long BlockHeight { get; set; }
}
