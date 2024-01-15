using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Seed.Dto;

public class CreateSeedDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string Seed { get; set; }
}