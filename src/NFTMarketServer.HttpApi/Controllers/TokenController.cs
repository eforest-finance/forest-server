using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Tokens;
using Volo.Abp;

namespace NFTMarketServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Token")]
    [Route("api/app/tokens")]
    public class TokenController : NFTMarketServerController
    {
        private readonly ITokenAppService _tokenAppService;

        public TokenController(ITokenAppService tokenAppService)
        {
            _tokenAppService = tokenAppService;
        }

        [HttpGet]
        [Route("market-data")]
        public Task<TokenMarketDataDto> GetTokenMarketDataAsync(string symbol)
        {
            return _tokenAppService.GetTokenMarketDataAsync(symbol, null);
        }
    }
}