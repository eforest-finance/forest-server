using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Market;
using NFTMarketServer.NFT;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Market")]
    [Route("api/app/market")]
    public class MarketController : NFTMarketServerController
    {
        private readonly INFTMarketDataAppService _nftMarketDataAppService;
        private readonly INFTListingAppService _nftListingAppService;
        private readonly INFTOfferAppService _nftOfferAppService;
        private readonly INFTInfoAppService _nftInfoAppService;

        public MarketController(INFTListingAppService nftListingAppService,
            INFTMarketDataAppService nftMarketDataAppService,
            INFTOfferAppService nftOfferAppService,
            INFTInfoAppService nftInfoAppService)
        {
            _nftListingAppService = nftListingAppService;
            _nftMarketDataAppService = nftMarketDataAppService;
            _nftOfferAppService = nftOfferAppService;
            _nftInfoAppService = nftInfoAppService;
        }

        [HttpGet]
        [Route("nft-market-data")]
        public Task<ListResultDto<NFTInfoMarketDataDto>> GetNFTInfoMarketDataAsync(GetNFTInfoMarketDataInput input)
        {
            return _nftMarketDataAppService.GetMarketDataAsync(input);
        }

        [HttpGet]
        [Route("nft-listings")]
        public Task<PagedResultDto<NFTListingIndexDto>> GetNFTListingsAsync(GetNFTListingsInput input)
        {
            return _nftListingAppService.GetNFTListingsAsync(input);
        }

        [HttpGet]
        [Route("nft-offers")]
        public Task<PagedResultDto<NFTOfferDto>> GetNFTOffersAsync(GetNFTOffersInput input)
        {
            return _nftOfferAppService.GetNFTOffersAsync(input);
        }
        
        [HttpPost]
        [Route("collected-collection-offers-made")]
        public Task<PagedResultDto<CollectedCollectionOffersDto>> GetCollectedCollectionOffersMadeAsync(GetCollectedCollectionOffersMadeInput input)
        {
            return _nftOfferAppService.GetCollectedCollectionOffersMadeAsync(input);
        }
        [HttpPost]
        [Route("collected-collection-received-offer")]
        public Task<PagedResultDto<CollectedCollectionOffersDto>> GetCollectedCollectionReceivedOfferAsync(GetCollectedCollectionReceivedOfferInput input)
        {
            return _nftOfferAppService.GetCollectedCollectionReceivedOfferAsync(input);
        }

        [HttpGet]
        [Route("symbol-info")]
        public Task<SymbolInfoDto> GetSymbolInfoAsync(GetSymbolInfoInput input )
        {
            return _nftInfoAppService.GetSymbolInfoAsync(input);
        }
    }
}