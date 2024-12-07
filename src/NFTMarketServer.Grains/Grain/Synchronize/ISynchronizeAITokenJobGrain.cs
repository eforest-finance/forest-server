using Orleans;

namespace NFTMarketServer.Grains.Grain.Synchronize;

public interface ISynchronizeAITokenJobGrain : IGrainWithStringKey
{
    Task<GrainResultDto<SynchronizeAITokenJobGrainDto>> CreateSynchronizeAITokenJobAsync(
        SaveSynchronizeAITokenJobGrainDto input);
    
    Task<GrainResultDto<SynchronizeAITokenJobGrainDto>> GetSynchronizeAITokenJobAsync();

    Task<GrainResultDto<SynchronizeAITokenJobGrainDto>> ExecuteJobAsync(SynchronizeTxJobGrainDto input);
    //Task<GrainResultDto<SynchronizeAITokenJobGrainDto>> CreateSeedJobAsync(CreateSeedJobGrainDto input);
}