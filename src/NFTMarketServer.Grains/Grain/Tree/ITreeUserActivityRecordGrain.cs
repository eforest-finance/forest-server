using Orleans;

namespace NFTMarketServer.Grains.Grain.Tree;

public interface ITreeUserActivityRecordGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TreeUserActivityRecordGrainDto>> GetTreeUserActivityRecordAsync();
    Task<GrainResultDto<TreeUserActivityRecordGrainDto>> SetTreeUserActivityRecordAsync(TreeUserActivityRecordGrainDto input);

}