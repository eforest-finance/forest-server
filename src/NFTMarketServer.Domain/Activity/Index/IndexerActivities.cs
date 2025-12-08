using System;
using System.Collections.Generic;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.Activity.Index;

public class IndexerActivities : IndexerCommonResult<IndexerActivities>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerActivity> IndexerActivityList { get; set; }
}

public class IndexerActivity : IndexerCommonResult<IndexerActivity>
{
    public DateTime TransactionDateTime { get; set; }
    public string Symbol { get; set; }
    public SymbolMarketActivityType Type { get; set; }
    public decimal Price { get; set; }
    public string PriceSymbol { get; set; }
    public decimal TransactionFee { get; set; }
    public string TransactionFeeSymbol { get; set; }
    public string TransactionId { get; set; }
}