using NFTMarketServer.Dealer.Dtos;
using Orleans;

namespace NFTMarketServer.Grains.Grain.Dealer.ContractInvoker;

public interface IContractInvokeGrain: IGrainWithGuidKey
{

    Task<GrainResultDto<ContractInvokeGrainDto>> UpdateAsync(ContractInvokeGrainDto input);

    Task<GrainResultDto<ContractInvokeGrainDto>> GetAsync();
    
}