using Orleans;

namespace NFTMarketServer.Grains.ApplicationHandler;

public interface IContractServiceGraphQLGrain : IGrainWithStringKey
{
    Task SetStateAsync(long height);

    Task<long> GetStateAsync();
}