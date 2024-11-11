using NFTMarketServer.Users.Index;
using Orleans;

namespace NFTMarketServer.Grains.Grain.Tree;

public interface ITreeUserPointsDetailGrain : IGrainWithStringKey
{
    Task<GrainResultDto<List<TreeGamePointsDetailInfoDto>>> GetTreeUserPointsDetailListAsync();
    Task<GrainResultDto<List<TreeGamePointsDetailInfoDto>>> SetTreeUserPointsDetailListAsync(List<TreeGamePointsDetailInfoDto> input);

}