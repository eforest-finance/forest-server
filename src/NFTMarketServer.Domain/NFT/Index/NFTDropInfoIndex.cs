using System;
using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class NFTDropInfoIndex : IndexerCommonResult<NFTDropInfoIndex>
{
    public string DropId { get; set; }
    public string CollectionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime ExpireTime { get; set; }
    public long ClaimMax { get; set; }
    public decimal ClaimPrice { get; set; }
    public string ClaimSymbol { get; set; }
    public long MaxIndex { get; set; }
    public long TotalAmount { get; set; }
    public long ClaimAmount { get; set; }
    public bool IsBurn { get; set; }
    public NFTDropState State { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public class NFTDropInfoIndexList : IndexerCommonResult<NFTDropInfoIndexList>
{
    public long TotalRecordCount { get; set; }
    public List<NFTDropInfoIndex> DropInfoIndexList { get; set; }
}