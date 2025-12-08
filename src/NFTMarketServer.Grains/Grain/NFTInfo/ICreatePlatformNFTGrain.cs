using NFTMarketServer.Grains.Grain.Users;
using NFTMarketServer.Users.Dto;
using Orleans;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public interface ICreatePlatformNFTGrain : IGrainWithStringKey
{
    Task<GrainResultDto<CreatePlatformNFTGrainDto>> GetCreatePlatformNFTAsync();

    Task<GrainResultDto<CreatePlatformNFTGrainDto>> SaveCreatePlatformNFTAsync(CreatePlatformNFTGrainInput createPlatformNFTGrainInput);
}