using System.Collections.Generic;
using NFTMarketServer.Seed.Dto;

namespace NFTMarketServer.Bid.Dtos;

public class AuctionInfoDto
{
    public string Id { get; set; }
    
    public string SeedSymbol { get; set; }

    public TokenPriceDto StartPrice { get; set; }

    public long StartTime { get; set; }

    public long EndTime { get; set; }

    public long MaxEndTime { get; set; }
    
    public int MinMarkup { get; set; }
    
    public int FinishIdentifier { get; set; }

    public string FinishBidder { get; set; }

    public long FinishTime { get; set; }
    
    public long Duration { get; set; }

    public TokenPriceDto FinishPrice { get; set; }
    
    public long BlockHeight { get; set; }
    
    public string Creator { get; set; }
    
    
    public string ReceivingAddress { get; set; }
    
    public string CollectionSymbol { get; set; }

    public TokenPriceDto StartUsdPrice { get; set; }
    
    public decimal CurrentUSDPrice { get; set; }
    
    public decimal CurrentELFPrice { get; set; }

    public string TransactionHash { get; set; }
    
    
    public decimal MinElfPriceMarkup { get; set; }
    public decimal MinDollarPriceMarkup { get; set; }
    
    
    public decimal CalculatorMinMarkup { get; set; }

}

public class SymbolAuctionRecordResultDto
{
    public List<AuctionInfoDto> GetSymbolAuctionInfos { get; set; }
}