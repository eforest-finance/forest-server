using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Market;

public class GetSymbolInfoInput
{
    [Required]
    public string Symbol { get; set; }
}