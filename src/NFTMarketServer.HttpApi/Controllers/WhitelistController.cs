using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Whitelist;
using NFTMarketServer.Whitelist.Dto;
using Volo.Abp;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Whitelist")]
[Route("api/app/whitelist")]
public class WhitelistController : NFTMarketServerController
{
    private readonly IWhitelistAppService _whitelistAppService;

    public WhitelistController(IWhitelistAppService whitelistAppService)
    {
        _whitelistAppService = whitelistAppService;
    }

    [HttpGet]
    [Route("hash")]
    public async Task<WhitelistInfoDto> GetWhitelistByHashAsync(GetWhitelistByHashDto input)
    {
        return await _whitelistAppService.GetWhitelistByHashAsync(input);
    }

    [HttpGet]
    [Route("extraInfoList")]
    public async Task<ExtraInfoIndexList> GetExtraInfoListAsync(GetWhitelistExtraInfoListDto input)
    {
        return await _whitelistAppService.GetWhitelistExtraInfoListAsync(input);
    }

    [HttpGet]
    [Route("whitelistManagers")]
    public async Task<WhitelistManagerList> GetManagerListAsync(GetWhitelistManagerListDto input)
    {
        return await _whitelistAppService.GetWhitelistManagerListAsync(input);
    }

    [HttpGet]
    [Route("tagInfos")]
    public async Task<WhitelistTagInfoList> GetTagListAsync(GetTagInfoListDto input)
    {
        return await _whitelistAppService.GetWhitelistTagInfoListAsync(input);
    }
}