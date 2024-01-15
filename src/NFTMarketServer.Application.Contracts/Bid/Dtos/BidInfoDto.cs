using System.Collections.Generic;

namespace NFTMarketServer.Bid.Dtos;

public class BidInfoDto
{
    public string Id { get; set; }
    public string AuctionId { get; set; }
    public long BlockHeight { get; set; }
    public string SeedSymbol { get; set; }

    public string Bidder { get; set; }

    public long PriceAmount { get; set; }

    public string PriceSymbol { get; set; }

    public long BidTime { get; set; }
    
    public string TransactionHash { get; set; }
    
    public decimal PriceUsdAmount { get; set; }
    
    public string PriceUsdSymbol { get; set; }
    public decimal MinDollarPriceMarkup { get; set; }
    public decimal MinElfPriceMarkup { get; set; }
    public decimal CalculatorMinMarkup { get; set; }
}

public class SymbolBidRecordResultDto
{
    public List<BidInfoDto> GetSymbolBidInfos { get; set; }
}