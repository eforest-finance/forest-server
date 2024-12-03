namespace NFTMarketServer.Grains.Grain.Users;
[GenerateSerializer]
public class CreatePlatformNFTGrainDto
{
    [Id(0)]
    public string Address { get; set; }
    [Id(1)]
    public int Count { get; set; }
}