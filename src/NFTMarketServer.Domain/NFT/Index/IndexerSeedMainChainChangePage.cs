using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerSeedMainChainChangePage : IndexerCommonResult<IndexerSeedMainChainChangePage>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerSeedMainChainChange> IndexerSeedMainChainChangeList { get; set; }
}

public class IndexerSeedMainChainChange : IndexerCommonResult<IndexerSeedMainChainChange>
{
    public string ChainId { get; set; }
    public string ToChainId { get; set; }
    public string Symbol { get; set; }
    public long BlockHeight { get; set; }
    public string TransactionId { get; set; }
}
