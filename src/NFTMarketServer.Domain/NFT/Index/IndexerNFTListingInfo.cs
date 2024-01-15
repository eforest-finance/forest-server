using System;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT;

public class IndexerNFTListingInfo
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

}