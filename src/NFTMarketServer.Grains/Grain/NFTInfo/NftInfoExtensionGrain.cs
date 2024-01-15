using NFTMarketServer.Grains.State.NFTInfo;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class NftInfoExtensionGrain : Grain<NftInfoExtensionState>, INftInfoExtensionGrain
{
    private readonly IObjectMapper _objectMapper;

    public NftInfoExtensionGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<NftInfoExtensionGrainDto>> CreateNftInfoExtensionAsync(NftInfoExtensionGrainDto input)
    {
        State = _objectMapper.Map<NftInfoExtensionGrainDto, NftInfoExtensionState>(input);

        await WriteStateAsync();

        return new GrainResultDto<NftInfoExtensionGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<NftInfoExtensionState, NftInfoExtensionGrainDto>(State)
        };
    }
}