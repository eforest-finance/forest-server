using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Synchronize;
using NFTMarketServer.Synchronize.Dto;
using Volo.Abp;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Synchronize")]
[Route("api/app/nft/")]
[Authorize]
public class SynchronizeController : NFTMarketServerController
{
    private readonly ISynchronizeAppService _synchronizeAppService;

    public SynchronizeController(ISynchronizeAppService synchronizeAppService)
    {
        _synchronizeAppService = synchronizeAppService;
    }

    [HttpGet]
    [Route("syncResult")]
    public Task<SyncResultDto> GetSyncResultByTxHashAsync(GetSyncResultByTxHashDto input)
    {
        return _synchronizeAppService.GetSyncResultByTxHashAsync(input);
    }
    [HttpGet]
    [AllowAnonymous]
    [Route("syncResultForAuctionSeed")]
    public Task<SyncResultDto> GetSyncResultForAuctionSeedByTxHashAsync(GetSyncResultByTxHashDto input)
    {
        return _synchronizeAppService.GetSyncResultForAuctionSeedByTxHashAsync(input);
    }

    [HttpPost]
    [Route("sync")]
    public Task<SendNFTSyncResponseDto> SendNFTSyncAsync(SendNFTSyncDto input)
    {
        return _synchronizeAppService.SendNFTSyncAsync(input);
    }
}