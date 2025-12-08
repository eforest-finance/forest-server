using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.SymbolMarketToken;
using NFTMarketServer.ThirdToken;
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
    private readonly IThirdTokenService _thirdTokenService;

    public SymbolMarketTokenController(ISymbolMarketTokenAppService symbolMarketTokenAppService,
        IThirdTokenService thirdTokenService)
    {
        _symbolMarketTokenAppService = symbolMarketTokenAppService;
        _thirdTokenService = thirdTokenService;
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

    [HttpGet]
    [Route("token-exist")]
    public async Task<SymbolMarketTokenExistDto> GetSymbolMarketTokenExistAsync(GetSymbolMarketTokenExistInput input)
    {
        return await _symbolMarketTokenAppService.GetSymbolMarketTokenExistAsync(input);
    }

    [HttpGet]
    [Route("my-third-token")]
    public async Task<MyThirdTokenResult> GetMyThirdTokenListAsync(GetMyThirdTokenInput input)
    {
        return await _thirdTokenService.GetMyThirdTokenListAsync(input);
    }

    [HttpPost]
    [Route("prepare-binding")]
    public async Task<ThirdTokenPrepareBindingDto> ThirdTokenPrepareBindingAsync(ThirdTokenPrepareBindingInput input)
    {
        return await _thirdTokenService.ThirdTokenPrepareBindingAsync(input);
    }

    [HttpPost]
    [Route("binding")]
    public async Task<string> ThirdTokenBindingAsync(ThirdTokenBindingInput input)
    {
        return await _thirdTokenService.ThirdTokenBindingAsync(input);
    }
}