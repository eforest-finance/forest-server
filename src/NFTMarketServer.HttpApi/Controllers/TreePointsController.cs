using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Dto;
using Volo.Abp;

namespace NFTMarketServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("User")]
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
        public Task<UserDto> GetUserTreeInfo(string address, string nickName)
        {
            return _userAppService.GetByUserAddressAsync(address);
        }

        [HttpPost]
        [Route("watering")]
        [Authorize]
        public Task WateringTree(UserUpdateDto input)
        {
            return _userAppService.UserUpdateAsync(input);
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