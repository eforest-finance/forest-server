using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerUserBalanceSync : IndexerCommonResult<IndexerUserBalanceSync>
{
    public long TotalRecordCount { get; set; }
    
    public List<UserBalanceIndex> DataList { get; set; }
}