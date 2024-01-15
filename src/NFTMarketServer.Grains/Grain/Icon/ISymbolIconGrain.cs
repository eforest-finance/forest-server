using Orleans;

namespace NFTMarketServer.Grains.Grain.Icon;

public interface ISymbolIconGrain : IGrainWithStringKey
{
    Task<GrainResultDto<SymbolIconGrainDto>> CreateSymbolIconAsync(string symbol, string icon);

    Task<GrainResultDto<SymbolIconGrainDto>> GetIconBySymbolAsync();
}