using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Trait;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Trait")]
[Route("api/app/trait")]
public class TraitController : NFTMarketServerController
{
    private readonly ITraitInfoAppService _traitInfoAppService;

    public TraitController(ITraitInfoAppService traitInfoAppService)
    {
        _traitInfoAppService = traitInfoAppService;
    }
    
    [HttpGet]
    [Route("nft-traits-info")]
    public async Task<NFTTraitsInfoDto> QueryNFTTraitsInfoAsync(QueryNFTTraitsInfoInput input)
    {
        return await _traitInfoAppService.QueryNFTTraitsInfoAsync(input);
    }

    [HttpPost]
    [Route("nft-collection-traits-info")]
    public async Task<PagedResultDto<NFTCollectionTraitInfoDto>> QueryNFTCollectionTraitsInfoAsync(
        QueryNFTCollectionTraitsInfoInput input)
    {
        return await _traitInfoAppService.QueryNFTCollectionTraitsInfoAsync(input);
    }
    
    [HttpGet]
    [Route("nft-collection-generation-info")]
    public async Task<CollectionGenerationInfoDto> QueryCollectionGenerationInfoAsync(QueryCollectionGenerationInfoInput input)
    {
        return await _traitInfoAppService.QueryCollectionGenerationInfoAsync(input);
    }
}