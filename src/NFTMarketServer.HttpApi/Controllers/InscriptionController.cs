using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Inscription;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("inscription")]
[Route("api/app/inscription")]
public class InscriptionController : AbpController
{
    private readonly IInscriptionAppService _inscriptionAppService;

    public InscriptionController(IInscriptionAppService inscriptionAppService)
    {
        _inscriptionAppService = inscriptionAppService;
    }

    [HttpPost]
    [Route("inscribed")]
    public async Task<InscribedDto> InscribedAsync(InscribedInput input)
    {
        return await _inscriptionAppService.InscribedAsync(input);
    }

    [HttpGet]
    [Route("inscription")]
    public async Task<InscriptionAmountDto> GetInscriptionAsync(GetInscriptionAmountInput input)
    {
        return await _inscriptionAppService.GetInscriptionAsync(input);
    }
}