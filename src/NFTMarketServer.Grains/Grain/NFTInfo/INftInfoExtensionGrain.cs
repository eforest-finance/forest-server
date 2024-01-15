using Orleans;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public interface INftInfoExtensionGrain : IGrainWithStringKey
{
    Task<GrainResultDto<NftInfoExtensionGrainDto>> CreateNftInfoExtensionAsync(NftInfoExtensionGrainDto input);
}