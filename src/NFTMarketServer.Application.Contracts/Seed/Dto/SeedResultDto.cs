namespace NFTMarketServer.Seed.Dto;

public class CreateSeedResultDto
{
    public bool Success { get; set; } = true;
    public string TxHash { get; set; }
}