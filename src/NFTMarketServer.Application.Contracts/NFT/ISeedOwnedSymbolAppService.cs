using System.Threading.Tasks;
using Autofac.Util;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public interface ISeedOwnedSymbolAppService
{
    Task<PagedResultDto<SeedSymbolIndexDto>> GetSeedOwnedSymbolsAsync(GetSeedOwnedSymbols input);
    
    Task<PagedResultDto<SeedSymbolIndexDto>> GetAllSeedOwnedSymbolsAsync(GetAllSeedOwnedSymbols input);
}