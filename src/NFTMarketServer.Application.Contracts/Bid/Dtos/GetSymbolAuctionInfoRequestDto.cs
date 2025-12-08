using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Bid.Dtos;

public class GetSymbolAuctionInfoRequestDto
{
    [Required]
    public string SeedSymbol { get; set; }
}