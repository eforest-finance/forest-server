using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Seed.Dto;

public class SearchSeedInput
{
    [Required] public string Symbol { get; set; }
    [Required] public string TokenType { get; set; }
}