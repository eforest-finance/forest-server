using System.Threading.Tasks;

namespace NFTMarketServer.File;

public interface ISymbolIconAppService
{
    Task<string> GetIconBySymbolAsync(string seedSymbol,string symbol);
}