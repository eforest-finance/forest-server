using Orleans;

namespace NFTMarketServer.Users.Dto;
[GenerateSerializer]
public class PlatformNFTTokenIdGrainInput
{
    [Id(0)]
    public string CollectionSymbol { get; set; }


    [Id(1)]
    public string TokenId { get; set; }
    
}