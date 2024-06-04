using System.Collections.Generic;
using System.Threading.Tasks;
using Castle.Core;

namespace NFTMarketServer.File;

public interface ISymbolIconAppService
{
    Task<string> GetIconBySymbolAsync(string seedSymbol, string symbol);

    Task<string> UpdateNFTIconAsync(byte[] utf8Bytes, string symbol);
    
    Task<KeyValuePair<string,string>> UpdateNFTIconWithHashAsync(byte[] utf8Bytes, string symbol);
}