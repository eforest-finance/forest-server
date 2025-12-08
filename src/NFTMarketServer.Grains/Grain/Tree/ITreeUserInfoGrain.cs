using NFTMarketServer.TreeGame;
using Orleans;

namespace NFTMarketServer.Grains.Grain.Tree;

public interface ITreeUserInfoGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TreeGameUserInfoDto>> GetTreeUserInfoAsync();
    Task<GrainResultDto<TreeGameUserInfoDto>> SetTreeUserInfoAsync(TreeGameUserInfoDto input);

}