using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public interface ISeedOwnedSymbolAppService
{
    Task<PagedResultDto<SeedSymbolIndexDto>> GetAllSeedOwnedSymbolsAsync(GetAllSeedOwnedSymbols input);
}