using NFTMarketServer.Grains.Grain.Tree;
using NFTMarketServer.Grains.State.NFTInfo;
using NFTMarketServer.TreeGame;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class TreeUserInfoGrain : Grain<TreeUserInfoState>, ITreeUserInfoGrain
{
    private readonly IObjectMapper _objectMapper;

    public TreeUserInfoGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public async Task<GrainResultDto<TreeGameUserInfoDto>> GetTreeUserInfoAsync()
    {
        return new GrainResultDto<TreeGameUserInfoDto>()
        {
            Success = true,
            Data = _objectMapper.Map<TreeUserInfoState, TreeGameUserInfoDto>(State)
        };
    }

    public async Task<GrainResultDto<TreeGameUserInfoDto>> SetTreeUserInfoAsync(TreeGameUserInfoDto input)
    {
        if (State == null || State.Id.IsNullOrEmpty())
        {
            State = new TreeUserInfoState
            {
                Id = input.Id,
                Address = input.Address,
                NickName = input.NickName,
                Points = input.Points,
                TreeLevel = input.TreeLevel,
                CreateTime = input.CreateTime,
                ParentAddress = input.ParentAddress,
                CurrentWater = input.CurrentWater,
                WaterUpdateTime = input.WaterUpdateTime
            };
        }
        else
        {
            State.ParentAddress = input.ParentAddress;
            State.TreeLevel = input.TreeLevel;
            State.WaterUpdateTime = input.WaterUpdateTime;
            State.Points = input.Points;
            State.CurrentWater = input.CurrentWater;
        }

        await WriteStateAsync();
        return new GrainResultDto<TreeGameUserInfoDto>()
        {
            Success = true,
            Data = _objectMapper.Map<TreeUserInfoState, TreeGameUserInfoDto>(State)
        };
    }
}