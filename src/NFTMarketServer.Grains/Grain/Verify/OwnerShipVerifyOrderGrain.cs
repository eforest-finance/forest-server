using Orleans;
using NFTMarketServer.Grains.State.Verify;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.Verify;

public class OwnerShipVerifyOrderGrain : Grain<OwnerShipVerifyOrderState>, IOwnerShipVerifyOrderGrain
{
    private readonly IObjectMapper _objectMapper;

    public OwnerShipVerifyOrderGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<OwnerShipVerifyOrderGrainDto>> GetAsync()
    {
        await ReadStateAsync();
        if (State.Id == Guid.Empty)
        {
            return new GrainResultDto<OwnerShipVerifyOrderGrainDto>
            {
                Success = false
            };
        }
        return new GrainResultDto<OwnerShipVerifyOrderGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<OwnerShipVerifyOrderState, OwnerShipVerifyOrderGrainDto>(State)
        }; 
    }

    public async Task<GrainResultDto<OwnerShipVerifyOrderGrainDto>> AddOrUpdateAsync(OwnerShipVerifyOrderGrainDto dto)
    {
        if (dto.Id == Guid.Empty)
        {
            dto.Id = Guid.NewGuid();
        }

        State = _objectMapper.Map<OwnerShipVerifyOrderGrainDto, OwnerShipVerifyOrderState>(dto);
        await WriteStateAsync();
        return new GrainResultDto<OwnerShipVerifyOrderGrainDto>
        {
            Success = true,
            Data = dto
        };
    }
}