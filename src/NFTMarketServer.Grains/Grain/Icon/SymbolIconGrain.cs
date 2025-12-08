using NFTMarketServer.Grains.State.Icon;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.Icon;

public class SymbolIconGrain : Grain<SymbolIconGrainState>, ISymbolIconGrain
{
    private readonly IObjectMapper _objectMapper;

    public SymbolIconGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<SymbolIconGrainDto>> CreateSymbolIconAsync(string symbol, string icon)
    {
        if (State == null || string.IsNullOrEmpty(State.Symbol))
        {
            State = new SymbolIconGrainState
            {
                Symbol = symbol,
                Icon = icon
            };
        }
        else
        {
            if (String.IsNullOrEmpty(State.Icon))
            {
                State.Icon = icon;
            }
        }

        await WriteStateAsync();
        return new GrainResultDto<SymbolIconGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<SymbolIconGrainState, SymbolIconGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<SymbolIconGrainDto>> GetIconBySymbolAsync()
    {
        if (State == null)
        {
            return null;
        }
        return new GrainResultDto<SymbolIconGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<SymbolIconGrainState, SymbolIconGrainDto>(State)
        };
    }
}