namespace NFTMarketServer.Seed.Dto;

public class BidPricePayInfoDto
{
    public decimal ElfBidPrice { get; set; }
    public decimal DollarBidPrice { get; set; }
    public decimal MinMarkup { get; set; }
    public decimal MinElfPriceMarkup { get; set; }
    public decimal MinDollarPriceMarkup { get; set; }
    public decimal DollarExchangeRate { get; set; }
}