namespace NFTMarketServer.Seed.Dto;

public class GetTsmUniqueSeedInfoRequestDto
{
    public SeedType SeedType { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
    public bool isBurn { get; set; }
    
    public SeedStatus Status { get; set; }
}