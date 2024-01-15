using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.SymbolMarketToken;

public interface ISymbolMarketTokenAppService
{
    Task<PagedResultDto<SymbolMarketTokenDto>> GetSymbolMarketTokensAsync(GetSymbolMarketTokenInput input);

    Task<SymbolMarketTokenIssuerDto> GetSymbolMarketTokenIssuerAsync(GetSymbolMarketTokenIssuerInput input);
}