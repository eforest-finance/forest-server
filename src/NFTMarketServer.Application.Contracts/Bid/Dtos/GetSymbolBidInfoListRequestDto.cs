using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Bid.Dtos;

public class GetSymbolBidInfoListRequestDto
{
    [Required]
    public string SeedSymbol { get; set; }
    
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}