using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.NFT;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Open")]
[Route("api/app/open")]
public class OpenController : NFTMarketServerController
{
    private readonly INFTCollectionAppService _nftCollectionAppService;
    
    [HttpPost]
    [Route("search-collections-floor-price")]
    public Task<PagedResultDto<SearchCollectionsFloorPriceDto>> SearchCollectionsFloorPriceAsync(SearchCollectionsFloorPriceInput input)
    {
        return _nftCollectionAppService.SearchCollectionsFloorPriceAsync(input);
    }
}