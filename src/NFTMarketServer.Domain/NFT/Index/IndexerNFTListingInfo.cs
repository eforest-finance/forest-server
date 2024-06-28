using System;
using System.Collections.Generic;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Index;


public class IndexerNFTListingInfos : IndexerCommonResult<IndexerNFTListingInfos>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerNFTListingInfo> IndexerNFTListingInfoList { get; set; }
}

public class IndexerNFTListingInfo : IndexerCommonResult<IndexerNFTListingInfo>
{
    public string Id { get; set; }
    public long Quantity { get; set; }
    public string Symbol { get; set; }
    public string Owner { get; set; }
    public string ChainId { get; set; }
    public decimal Prices { get; set; }
    public decimal? WhitelistPrices { get; set; }
    public string WhitelistId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime PublicTime { get; set; }
    public DateTime ExpireTime { get; set; }
    public IndexerTokenInfo PurchaseToken { get; set; }
    public IndexerNFTInfo NftInfo { get; set; }
    public IndexerNFTCollection NftCollectionDto { get; set; }
    public long RealQuantity { get; set; }
    public string BusinessId { get; set; }

}