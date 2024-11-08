using NFTMarketServer.Grains.Grain.Tree;
using NFTMarketServer.Grains.State.NFTInfo;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class TreeUserActivityRecordGrain : Grain<TreeUserActivityRecordState>, ITreeUserActivityRecordGrain
{
    private readonly IObjectMapper _objectMapper;

    public TreeUserActivityRecordGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public async Task<GrainResultDto<TreeUserActivityRecordGrainDto>> GetTreeUserActivityRecordAsync()
    {
        if (State == null || State.Id.IsNullOrEmpty())
        {
            return null;
        }
        return new GrainResultDto<TreeUserActivityRecordGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<TreeUserActivityRecordState, TreeUserActivityRecordGrainDto>(State)
        };
    }


    public async Task<GrainResultDto<TreeUserActivityRecordGrainDto>> SetTreeUserActivityRecordAsync(TreeUserActivityRecordGrainDto input)
    {
        if (State == null || State.Id.IsNullOrEmpty())
        {
            return null;
        }
        else
        {
            State.Id = input.Id;
            State.Address = input.Address;
            State.ActivityId = input.ActivityId;
            State.ClaimCount = input.ClaimCount;
        }

        await WriteStateAsync();
        return new GrainResultDto<TreeUserActivityRecordGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<TreeUserActivityRecordState, TreeUserActivityRecordGrainDto>(State)
        };
    }
}