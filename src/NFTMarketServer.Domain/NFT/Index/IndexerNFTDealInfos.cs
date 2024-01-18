using System;
using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTDealInfos : IndexerCommonResult<IndexerNFTDealInfos>
{
    public long TotalRecordCount { get; set; }

    public List<IndexerNFTDealInfo> IndexerNftDealList { get; set; } = new List<IndexerNFTDealInfo>();
}

public class IndexerNFTDealInfo : IndexerCommonResult<IndexerNFTDealInfo>
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string NftFrom { get; set; }
    public string NftTo { get; set; }
    public string NftSymbol { get; set; }
    public string NftQuantity { get; set; }
    public string PurchaseSymbol { get; set; }
    public string PurchaseTokenId { get; set; }
    public string NftInfoId { get; set; }
    public long PurchaseAmount { get; set; }
    public DateTime DealTime { get; set; }
    public string CollectionSymbol { get; set; }
}