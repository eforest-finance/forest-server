using Orleans;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public interface INFTCollectionExtensionGrain : IGrainWithStringKey
{
    Task<GrainResultDto<NftCollectionExtensionGrainDto>> CreateNftCollectionExtensionAsync(NftCollectionExtensionGrainDto input);
    
    Task<GrainResultDto<NftCollectionExtensionGrainDto>> UpdateNftCollectionExtensionAsync(NftCollectionExtensionGrainDto input);

    Task<GrainResultDto<NftCollectionExtensionGrainDto>> GetAsync();
}