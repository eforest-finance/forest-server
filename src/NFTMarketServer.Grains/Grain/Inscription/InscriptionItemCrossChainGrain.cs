using Microsoft.Extensions.Logging;
using NFTMarketServer.Grains.State.Inscription;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.Inscription;

public class InscriptionItemCrossChainGrain : Grain<InscriptionItemCrossChainState>, IInscriptionItemCrossChainGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<InscriptionItemCrossChainGrain> _logger;

    public InscriptionItemCrossChainGrain(IObjectMapper objectMapper,
        ILogger<InscriptionItemCrossChainGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<GrainResultDto<InscriptionItemCrossChainGrainDto>> SaveItemCrossChainTransactionAsync(
        InscriptionItemCrossChainGrainDto inscriptionItemCrossChainGrainDto)
    {
        State = _objectMapper.Map<InscriptionItemCrossChainGrainDto, InscriptionItemCrossChainState>(
            inscriptionItemCrossChainGrainDto);
        await WriteStateAsync();
        return new GrainResultDto<InscriptionItemCrossChainGrainDto>
        {
            Success = true,
            Data = inscriptionItemCrossChainGrainDto
        };
    }

    public async Task<GrainResultDto<InscriptionItemCrossChainGrainDto>> SaveCollectionCreated(bool isCollectionCreated)
    {
        State.IsCollectionCreated = isCollectionCreated;
        await WriteStateAsync();
        return new GrainResultDto<InscriptionItemCrossChainGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<InscriptionItemCrossChainState, InscriptionItemCrossChainGrainDto>(State)
        };
    }
}