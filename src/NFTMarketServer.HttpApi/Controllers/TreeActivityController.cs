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
    [Route("generate-id")]
    public async Task GetIdAsync(
    )
    {
        await _treeService.CreateTreeActivityAsync(request);
    }
    
    [HttpPost]
    [Authorize]
    [Route("create")]
    public async Task CreateTreeActivityAsync(CreateTreeActivicyRequest request
    )
    {
        await _treeService.CreateTreeActivityAsync(request);
    }
    
}