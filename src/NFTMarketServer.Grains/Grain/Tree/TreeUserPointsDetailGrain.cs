using NFTMarketServer.Grains.Grain.Tree;
using NFTMarketServer.Grains.State.NFTInfo;
using NFTMarketServer.Users.Index;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class TreeUserPointsDetailGrain : Grain<TreeUserPointsDetailState>, ITreeUserPointsDetailGrain
{
    private readonly IObjectMapper _objectMapper;

    public TreeUserPointsDetailGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<List<TreeGamePointsDetailInfoDto>>> GetTreeUserPointsDetailListAsync()
    {
        if (State == null || State.TreeGamePointsDetailInfoDtos.IsNullOrEmpty()) return null;
        return new GrainResultDto<List<TreeGamePointsDetailInfoDto>>()
        {
            Success = true,
            Data = State.TreeGamePointsDetailInfoDtos
        };
    }

    public async Task<GrainResultDto<List<TreeGamePointsDetailInfoDto>>> SetTreeUserPointsDetailListAsync(List<TreeGamePointsDetailInfoDto> input)
    {
        State.TreeGamePointsDetailInfoDtos = input;
        await WriteStateAsync();
        return new GrainResultDto<List<TreeGamePointsDetailInfoDto>>()
        {
            Success = true,
            Data = State.TreeGamePointsDetailInfoDtos
        };
    }
}