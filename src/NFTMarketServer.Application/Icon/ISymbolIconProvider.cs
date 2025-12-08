using System.Threading.Tasks;

namespace NFTMarketServer.Icon;

public interface ISymbolIconProvider
{
    Task<string> GetIconBySymbolAsync(string symbol);
    
    
    Task<string> AddSymbolIconAsync(string symbol,string icon);
}