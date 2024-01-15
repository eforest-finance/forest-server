using Orleans;

namespace NFTMarketServer.Grains.Grain.Inscription;

public interface IInscriptionItemCrossChainGrain : IGrainWithStringKey
{
      public Task<GrainResultDto<InscriptionItemCrossChainGrainDto>> SaveItemCrossChainTransactionAsync(
            InscriptionItemCrossChainGrainDto inscriptionItemCrossChainGrainDto);

      public Task<GrainResultDto<InscriptionItemCrossChainGrainDto>> SaveCollectionCreated(bool isCollectionCreated);
}