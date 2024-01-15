using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.SymbolMarketToken;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("SymbolMarketToken")]
[Route("api/app/token")]
public class SymbolMarketTokenController : AbpController
{
    private readonly ISymbolMarketTokenAppService _symbolMarketTokenAppService;

    public SymbolMarketTokenController(ISymbolMarketTokenAppService symbolMarketTokenAppService)
    {
        _symbolMarketTokenAppService = symbolMarketTokenAppService;
    }
    
    [HttpGet]
    [Route("my-token")]
    public async Task<PagedResultDto<SymbolMarketTokenDto>> GetSymbolMarketTokensAsync(GetSymbolMarketTokenInput input)
    {
        return await _symbolMarketTokenAppService.GetSymbolMarketTokensAsync(input);
    }
    
    [HttpGet]
    [Route("token-issuer")]
    [Authorize]
    public async Task<SymbolMarketTokenIssuerDto> GetSymbolMarketTokenIssuerAsync(GetSymbolMarketTokenIssuerInput input)
    {
        return await _symbolMarketTokenAppService.GetSymbolMarketTokenIssuerAsync(input);
    }
}