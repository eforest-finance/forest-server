using NFTMarketServer.Grains.State.NFTInfo;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class NftCollectionExtensionGrain : Grain<NftCollectionExtensionState>, INFTCollectionExtensionGrain
{
    private readonly IObjectMapper _objectMapper;

    public NftCollectionExtensionGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    public async Task<GrainResultDto<NftCollectionExtensionGrainDto>> CreateNftCollectionExtensionAsync(NftCollectionExtensionGrainDto input)
    {
        State = _objectMapper.Map<NftCollectionExtensionGrainDto, NftCollectionExtensionState>(input);

        await WriteStateAsync();

        return new GrainResultDto<NftCollectionExtensionGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<NftCollectionExtensionState, NftCollectionExtensionGrainDto>(State)

        };
    }

    public async Task<GrainResultDto<NftCollectionExtensionGrainDto>> UpdateNftCollectionExtensionAsync(NftCollectionExtensionGrainDto input)
    {
        State = _objectMapper.Map<NftCollectionExtensionGrainDto, NftCollectionExtensionState>(input);
        
        await WriteStateAsync();
        
        return new GrainResultDto<NftCollectionExtensionGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<NftCollectionExtensionState, NftCollectionExtensionGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<NftCollectionExtensionGrainDto>> GetAsync()
    {
        return new GrainResultDto<NftCollectionExtensionGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<NftCollectionExtensionState, NftCollectionExtensionGrainDto>(State)
        };
    }
}