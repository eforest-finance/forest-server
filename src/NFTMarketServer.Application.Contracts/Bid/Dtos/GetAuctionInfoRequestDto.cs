namespace NFTMarketServer.Bid.Dtos;

public class GetAuctionInfoRequestDto
{
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}