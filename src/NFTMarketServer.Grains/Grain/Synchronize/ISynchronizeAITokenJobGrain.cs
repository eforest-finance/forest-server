
namespace NFTMarketServer.Grains.Grain.Synchronize.Ai;

public interface ISynchronizeAITokenJobGrain : IGrainWithStringKey
{
    Task<GrainResultDto<SynchronizeAITokenJobGrainDto>> CreateSynchronizeAITokenJobAsync(
        SaveSynchronizeAITokenJobGrainDto input);
    
    Task<GrainResultDto<SynchronizeAITokenJobGrainDto>> GetSynchronizeAITokenJobAsync();

    Task<GrainResultDto<SynchronizeAITokenJobGrainDto>> ExecuteJobAsync(SynchronizeAITokenJobGrainDto input);

}