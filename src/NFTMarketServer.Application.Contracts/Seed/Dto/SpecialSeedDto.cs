namespace NFTMarketServer.Seed.Dto;

public class SpecialSeedDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public int SymbolLength => Symbol.Length;

    public string SeedName { get; set; }
    public string SeedImage { get; set; }
    public SeedStatus Status { get; set; }
    public TokenType TokenType { get; set; }
    public SeedType SeedType { get; set; }
    public AuctionType AuctionType { get; set; }
    public TokenPriceDto TokenPrice { get; set; }
    public TokenPriceDto TopBidPrice { get; set; }
    public long AuctionEndTime { get; set; }
    public int BidsCount { get; set; }
    public int BiddersCount { get; set; }
}