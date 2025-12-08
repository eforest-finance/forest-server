using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.NFT;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("NFT")]
    [Route("api/app/drop")]
    public class NFTDropController : NFTMarketServerController
    {
        private readonly INFTDropAppService _nftDropAppService;
        
        public NFTDropController(INFTDropAppService nftDropAppService
            )
        {
            _nftDropAppService = nftDropAppService;
        }
        

        [HttpPost]
        [Authorize]
        [Route("drop-infos")]
        public Task CreateNFTDropAsync(CreateNFTDropInput input)
        {
            return _nftDropAppService.CreateNFTDropExtensionAsync(input);
        }

        
        [HttpGet]
        [Route("list")]
        public Task<PagedResultDto<NFTDropIndexDto>> GetNFTDropListAsync(GetNFTDropListInput input)
        {
            return _nftDropAppService.GetNFTDropListAsync(input);
        }
        
        
        [HttpGet]
        [Route("recommendation")]
        public Task<List<RecommendedNFTDropIndexDto>> GetRecommendedNFTCcollectionsAsync()
        {
            return _nftDropAppService.GetRecommendedNFTDropListAsync();
        }
        
        [HttpGet]
        [Route("detail")]
        public Task<NFTDropDetailDto> GetNFTDropDetailAsync(GetNFTDropDetailInput input)
        {
            return _nftDropAppService.GetNFTDropDetailAsync(input);
        }
        
        [HttpGet]
        [Route("quota")]
        public Task<NFTDropQuotaDto> GetNFTDropQuotaAsync(GetNFTDropQuotaInput input)
        {
            return _nftDropAppService.GetNFTDropQuotaAsync(input);
        }
    }
}