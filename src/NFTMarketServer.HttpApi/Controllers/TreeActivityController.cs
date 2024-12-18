using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Tree;
using Volo.Abp;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("TreeActive")]
[Route("api/app/tree")]
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
    public async Task<string> GetIdAsync(
    )
    {
        return await _treeService.GenerateIdAsync();
    }

    [HttpPost]
    [Authorize]
    [Route("activity-create")]
    public async Task<TreeActivityIndex> CreateTreeActivityAsync(CreateTreeActivityRequest request
    )
    {
        return await _treeService.CreateTreeActivityAsync(request);
    }
    
    [HttpGet]
    [Authorize]
    [Route("activity-create")]
    public async Task<string> CreateNewTreeActivityIdAsync(
    )
    {
        return await _treeService.CreateNewTreeActivityIdAsync();
    }

    [HttpPost]
    [Authorize]
    [Route("activity-modify-hide-flag")]
    public async Task<bool> ModifyTreeActivityHideFlagAsync(ModifyTreeActivityHideFlagRequest request
    )
    {
        return await _treeService.ModifyTreeActivityHideFlagAsync(request);
    }
    
    [HttpPost]
    [Authorize]
    [Route("activity-modify-status")]
    public async Task<bool> ModifyTreeActivityStatusAsync(ModifyTreeActivityStatusRequest request
    )
    {
        return await _treeService.ModifyTreeActivityStatusAsync(request);
    }
    
    [HttpGet]
    [Route("activity-list")]
    public async Task<List<TreeActivityDto>> GetTreeActivityListAsync(GetTreeActivityListInput request)
    {
        return await _treeService.GetTreeActivityListAsync(request);
    }
    [HttpGet]
    [Route("activity-detail")]
    public async Task<TreeActivityDto> GetTreeActivityDetailAsync(string id, string address
    )
    {
        return await _treeService.GetTreeActivityDetailAsync(id, address);
    }
}