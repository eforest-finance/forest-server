using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Activity;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

// [RemoteService]
// [Area("app")]
// [ControllerName("Activity")]
//  [Route("api/app/activity")]
public class ActivityController : AbpController
{
    private readonly IActivityAppService _activityAppService;

    public ActivityController(IActivityAppService activityAppService)
    {
        _activityAppService = activityAppService;
    }
    
    [HttpGet]
    [Route("my-activity")]
    public async Task<PagedResultDto<SymbolMarketActivityDto>> GetActivitiesAsync(GetActivitiesInput input)
    {
        return await _activityAppService.GetListAsync(input);
    }
}