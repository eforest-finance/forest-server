using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Chains;
using Volo.Abp;

namespace NFTMarketServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Chain")]
    [Route("api/app/chains")]
    public class ChainController : NFTMarketServerController
    {
        private readonly IChainAppService _chainAppService;

        public ChainController(IChainAppService chainAppService)
        {
            _chainAppService = chainAppService;
        }

        [HttpGet]
        public Task<string[]> GetChainsAsync()
        {
            return _chainAppService.GetListAsync();
        }
    }
}