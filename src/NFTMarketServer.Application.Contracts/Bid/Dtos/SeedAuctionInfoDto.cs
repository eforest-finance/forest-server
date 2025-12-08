using System.Collections.Generic;
using NFTMarketServer.Seed.Dto;

namespace NFTMarketServer.Bid.Dtos;

public class SeedAuctionInfoDto
{
    public long AuctionEndTime { get; set; }
    
    public List<string> BidderList { get; set; }
    
    public TokenPriceDto TopBidPrice  { get; set; }
}