using System.IO;
using System.Threading.Tasks;

namespace NFTMarketServer.File;

public interface ISymbolIconAppService
{
    Task<string> GetIconBySymbolAsync(string seedSymbol, string symbol);

    Task<string> UpdateNFTIconAsync(byte[] utf8Bytes, string symbol);
}