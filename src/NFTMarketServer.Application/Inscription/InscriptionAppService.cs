using System;
using System.Threading.Tasks;
using NFTMarketServer.Grains.Grain.Inscription;
using NFTMarketServer.Grains.State.Inscription;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Inscription;

public class InscriptionAppService : NFTMarketServerAppService, IInscriptionAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;

    public InscriptionAppService(IClusterClient clusterClient, IObjectMapper objectMapper)
    {
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
    }

    public async Task<InscribedDto> InscribedAsync(InscribedInput input)
    {
        var uid = Guid.NewGuid();
        var grainResult = await _clusterClient.GetGrain<IInscriptionInscribeGrain>(uid)
            .SaveInscription(input.RawTransaction);
        // getTransactionResult async
        _ = _clusterClient.GetGrain<IInscriptionInscribeGrain>(uid)
            .UpdateInscriptionStatus();
        return new InscribedDto
        {
            TransactionId = grainResult.Data.TransactionId
        };
    }

    public async Task<InscriptionAmountDto> GetInscriptionAsync(GetInscriptionAmountInput input)
    {
        var grainResult = await _clusterClient.GetGrain<IInscriptionAmountGrain>(input.Tick).QueryAmount();
        return _objectMapper.Map<InscriptionAmountGrainDto, InscriptionAmountDto>(grainResult.Data);
    }
}