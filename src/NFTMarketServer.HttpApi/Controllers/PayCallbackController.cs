using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Order;
using NFTMarketServer.Order.Dto;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("PayCallback")]
[Route("api/app/pay")]
public class PayCallbackController : AbpController
{
    private readonly IPayCallbackAppService _payCallbackAppService;

    public PayCallbackController(IPayCallbackAppService payCallbackAppService)
    {
        _payCallbackAppService = payCallbackAppService;
    }

    [HttpPost]
    [Route("portkey/callback")]
    public virtual Task<bool> PortkeyCallback(PortkeyCallbackInput input)
    {
        return _payCallbackAppService.PortkeyOrderCallbackAsync(input);
    }
}