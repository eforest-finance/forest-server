using Orleans;

namespace NFTMarketServer.Grains.Grain.Inscription;

public interface IInscriptionInscribeGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<InscriptionInscribeGrainDto>> SaveInscription(string rawTransaction);
    Task UpdateInscriptionStatus();
}