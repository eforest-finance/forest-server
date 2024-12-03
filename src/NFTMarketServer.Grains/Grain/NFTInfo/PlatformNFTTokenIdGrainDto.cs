namespace NFTMarketServer.Grains.Grain.Users;
[GenerateSerializer]
public class PlatformNFTTokenIdGrainDto
{
    [Id(0)]
    public string TokenId { get; set; }
}