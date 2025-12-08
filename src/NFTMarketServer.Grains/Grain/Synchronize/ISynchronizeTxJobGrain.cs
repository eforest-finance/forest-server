using Orleans;

namespace NFTMarketServer.Grains.Grain.Synchronize;

public interface ISynchronizeTxJobGrain : IGrainWithStringKey
{
    Task<GrainResultDto<SynchronizeTxJobGrainDto>> CreateSynchronizeTransactionJobAsync(
        CreateSynchronizeTransactionJobGrainDto input);

    Task<GrainResultDto<SynchronizeTxJobGrainDto>> ExecuteJobAsync(SynchronizeTxJobGrainDto input);
    Task<GrainResultDto<SynchronizeTxJobGrainDto>> CreateSeedJobAsync(CreateSeedJobGrainDto input);
}