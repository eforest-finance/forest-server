using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Tree;
using NFTMarketServer.TreeGame;
using NFTMarketServer.Users;
using Volo.Abp;

namespace NFTMarketServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Tree")]
    [Route("api/app/tree")]
    public class TreePointsController : NFTMarketServerController
    {
        private readonly IUserAppService _userAppService;
        private readonly ITreeGameService _treeGameService;

        public TreePointsController(IUserAppService userAppService, ITreeGameService treeGameService)
        {
            _userAppService = userAppService;
            _treeGameService = treeGameService;
        }

        [HttpGet]
        [Route("user-info")]
        public async Task<TreeGameHomePageInfoDto> GetUserTreeInfo(string address, string nickName)
        {
            return await _treeGameService.GetUserTreeInfoAsync(address, nickName, true);
        }

        [HttpPost]
        [Route("watering")]
        [Authorize]
        public async Task<TreeGameHomePageInfoDto> WateringTree(TreeWateringRequest input)
        {
            return await _treeGameService.WateringTreeAsync(input);
        }
        
        [HttpPost]
        [Route("level-update")]
        //[Authorize]
        public Task<TreeLevelUpgradeOutput> TreeUpdateLevel(TreeLevelUpdateRequest request)
        {
            return _treeGameService.UpgradeTreeLevelAsync(request);
        }
        
        [HttpPost]
        [Route("claim")]
        //[Authorize]
        public Task<TreePointsClaimOutput> PointsClaim(TreePointsClaimRequest request)
        {
            return _treeGameService.ClaimAsync(request);
        }
        
        
        [HttpPost]
        [Route("points-convert")]
        //[Authorize]
        public Task<TreePointsConvertOutput> PointsConvert(TreePointsConvertRequest request)
        {
            return _treeGameService.PointsConvertAsync(request);
        }
        
        [HttpGet]
        [Route("count")]
        public Task<long> CountAsync(DateTime beginTime, DateTime endTime)
        {
            return _userAppService.GetUserCountAsync(beginTime, endTime);
        }
    }
}