using Orleans;

namespace NFTMarketServer.Grains.Grain.Inscription;

public interface IInscriptionAmountGrain : IGrainWithStringKey
{
    Task<GrainResultDto<InscriptionAmountGrainDto>> AddAmount(string tick, long amount);

    Task<GrainResultDto<InscriptionAmountGrainDto>> UpdateAmount(string tick, long amount);

    Task<GrainResultDto<InscriptionAmountGrainDto>> QueryAmount();
}