using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Grains.Grain.Icon;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Icon;

public class SymbolIconProvider : ISymbolIconProvider,ISingletonDependency
{
    private readonly ILogger<SymbolIconProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;


    public SymbolIconProvider(IClusterClient clusterClient, IObjectMapper objectMapper)
    {
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
    }

    public async Task<string> GetIconBySymbolAsync(string symbol)
    {
        var symbolIconGrain = _clusterClient.GetGrain<ISymbolIconGrain>(symbol);
        var result = await symbolIconGrain.GetIconBySymbolAsync();
        return result == null ? string.Empty : result.Data.Icon;
    }

    public async Task<string> AddSymbolIconAsync(string symbol, string icon)
    {
        var symbolIconGrain = _clusterClient.GetGrain<ISymbolIconGrain>(symbol);
        var resultDto = await symbolIconGrain.CreateSymbolIconAsync(symbol, icon);
        return resultDto.Data.Icon;
    }
}