using System;

namespace NFTMarketServer.NFT.Index;

public class IndexerListingWhitelistPrice
{
    public string ListingId { get; set; }
    public long Quantity { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime PublicTime { get; set; }
    public DateTime ExpireTime { get; set; }
    public long DurationHours { get; set; }
    public string OfferFrom { get; set; }
    public string NftInfoId { get; set; }
    public string Owner { get; set; }
    public decimal Prices { get; set; }
    public decimal? WhiteListPrice { get; set; }
    public string WhitelistId { get; set; }
    public IndexerTokenInfo WhitelistPriceToken { get; set; }
}