using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Synchronize;
using NFTMarketServer.Synchronize.Dto;
using Volo.Abp;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Synchronize")]
[Route("api/app/nft/")]
public class AISynchronizeController : NFTMarketServerController
{
    private readonly ISynchronizeAppService _synchronizeAppService;

    public AISynchronizeController(ISynchronizeAppService synchronizeAppService)
    {
        _synchronizeAppService = synchronizeAppService;
    }
    
    [HttpPost]
    [Route("sync-ai")]
    public Task<SendNFTSyncResponseDto> SendAITokenSyncAsync(SendNFTAISyncDto input)
    {
        return _synchronizeAppService.AddAITokenSyncJobAsync(input);
    }
}