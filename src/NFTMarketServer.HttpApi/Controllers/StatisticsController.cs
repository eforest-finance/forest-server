using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Ai;
using NFTMarketServer.NFT;
using Volo.Abp;

namespace NFTMarketServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("NFT")]
    [Route("api/app/statistics")]
    public class StatisticsController : NFTMarketServerController
    {
        private readonly INFTInfoAppService _nftAppService;
        private readonly INFTCollectionAppService _nftCollectionAppService;
        private readonly INFTActivityAppService _nftActivityAppService;
        private readonly ISeedOwnedSymbolAppService _seedOwnedSymbolAppService;
        private readonly IAiAppService _aiAppService;
        private readonly IStatisticsAppService _statisticsAppService;



        public StatisticsController(
            IAiAppService aiAppService,
            INFTInfoAppService nftAppService,
            INFTCollectionAppService nftCollectionAppService,
            INFTActivityAppService nftActivityAppService, 
            ISeedOwnedSymbolAppService seedOwnedSymbolAppService,
            IStatisticsAppService statisticsAppService)
        {
            _nftAppService = nftAppService;
            _nftCollectionAppService = nftCollectionAppService;
            _nftActivityAppService = nftActivityAppService;
            _seedOwnedSymbolAppService = seedOwnedSymbolAppService;
            _aiAppService = aiAppService;
            _statisticsAppService = statisticsAppService;
        }
        
        [HttpGet]
        [Route("newuser")]
        public Task<long> GetNewUserCountAsync(GetNewUserInput input)
        {
            return _statisticsAppService.GetListAsync(input);
        }

        
    }
}