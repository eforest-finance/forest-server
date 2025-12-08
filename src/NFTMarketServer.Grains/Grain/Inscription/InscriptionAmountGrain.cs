using Microsoft.Extensions.Logging;
using NFTMarketServer.Grains.State.Inscription;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.Inscription;

public class InscriptionAmountGrain : Grain<InscriptionAmountState>, IInscriptionAmountGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<InscriptionAmountGrain> _logger;

    public InscriptionAmountGrain(IObjectMapper objectMapper,
        ILogger<InscriptionAmountGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<GrainResultDto<InscriptionAmountGrainDto>> AddAmount(string tick, long amount)
    {
        if (State.Id.IsNullOrEmpty())
        {
            State.Id = tick;
        }

        State.Tick = tick;
        State.TotalAmount += amount;
        await WriteStateAsync();
        return new GrainResultDto<InscriptionAmountGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<InscriptionAmountState, InscriptionAmountGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<InscriptionAmountGrainDto>> UpdateAmount(string tick, long amount)
    {
        if (State.Id.IsNullOrEmpty())
        {
            State.Id = tick;
        }

        State.Tick = tick;
        State.TotalAmount = amount;
        await WriteStateAsync();
        return new GrainResultDto<InscriptionAmountGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<InscriptionAmountState, InscriptionAmountGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<InscriptionAmountGrainDto>> QueryAmount()
    {
        return new GrainResultDto<InscriptionAmountGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<InscriptionAmountState, InscriptionAmountGrainDto>(State)
        };
    }
}