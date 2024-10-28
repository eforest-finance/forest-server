using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.TreeGame;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Dto;
using NFTMarketServer.Users.Index;
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
        public async Task<TreeGameHomePageInfoDto> WateringTree(string address, int count)
        {
            return await _treeGameService.WateringTreeAsync(address, count);
        }
        
        [HttpPost]
        [Route("claim")]
        [Authorize]
        public Task PointsClaim(UserUpdateDto input)
        {
            return _userAppService.UserUpdateAsync(input);
        }
        
        [HttpPost]
        [Route("level-update")]
        [Authorize]
        public Task TreeUpdateLevel(UserUpdateDto input)
        {
            return _userAppService.UserUpdateAsync(input);
        }
        
        [HttpPost]
        [Route("points-convert")]
        [Authorize]
        public Task PointsConvert(UserUpdateDto input)
        {
            return _userAppService.UserUpdateAsync(input);
        }
        
        [HttpGet]
        [Route("count")]
        public Task<long> CountAsync(DateTime beginTime, DateTime endTime)
        {
            return _userAppService.GetUserCountAsync(beginTime, endTime);
        }
    }
}