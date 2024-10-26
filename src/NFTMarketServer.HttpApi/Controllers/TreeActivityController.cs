using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.NFT;
using NFTMarketServer.Tree;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("TreeActive")]
[Route("api/app/tree/activety")]
public class TreeActivityController : NFTMarketServerController
{
    private readonly ITreeService _treeService;
    
    public TreeActivityController(ITreeService treeService)
    {
        _treeService = treeService;
    }

    [HttpGet]
    [Authorize]
    [Route("generate-id")]
    public async Task GetIdAsync(
    )
    {
        await _treeService.GenerateId();
    }

    [HttpPost]
    [Authorize]
    [Route("create")]
    public async Task CreateTreeActivityAsync(CreateTreeActivityRequest request
    )
    {
        await _treeService.CreateTreeActivityAsync(request);
    }

    [HttpGet]
    [Authorize]
    [Route("modify-hide-flag")]
    public async Task ModifyTreeActivityHideFlagAsync(ModifyTreeActivityHideFlagRequest request
    )
    {
        await _treeService.ModifyTreeActivityHideFlagAsync(request);
    }
    
    [HttpGet]
    [Authorize]
    [Route("modify-status")]
    public async Task ModifyTreeActivityStatusAsync(ModifyTreeActivityStatusRequest request
    )
    {
        await _treeService.ModifyTreeActivityStatusAsync(request);
    }
}