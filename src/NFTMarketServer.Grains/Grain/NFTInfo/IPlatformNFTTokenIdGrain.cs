using NFTMarketServer.Grains.Grain.Users;
using NFTMarketServer.Users.Dto;
using Orleans;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public interface IPlatformNFTTokenIdGrain : IGrainWithStringKey
{
    
    Task<GrainResultDto<PlatformNFTTokenIdGrainDto>> GetPlatformNFTCurrentTokenIdAsync();
    
    Task<GrainResultDto<PlatformNFTTokenIdGrainDto>> SavePlatformNFTTokenIdAsync(PlatformNFTTokenIdGrainInput clatformNFTTokenIdGrainInput);

}