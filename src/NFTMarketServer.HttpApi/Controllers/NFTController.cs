using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Models;
using NFTMarketServer.NFT;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("NFT")]
    [Route("api/app/nft")]
    public class NFTController : NFTMarketServerController
    {
        private readonly INFTInfoAppService _nftAppService;
        private readonly INFTCollectionAppService _nftCollectionAppService;
        private readonly INFTActivityAppService _nftActivityAppService;
        private readonly ISeedOwnedSymbolAppService _seedOwnedSymbolAppService;


        public NFTController(INFTInfoAppService nftAppService,
            INFTCollectionAppService nftCollectionAppService,
            INFTActivityAppService nftActivityAppService, 
            ISeedOwnedSymbolAppService seedOwnedSymbolAppService)
        {
            _nftAppService = nftAppService;
            _nftCollectionAppService = nftCollectionAppService;
            _nftActivityAppService = nftActivityAppService;
            _seedOwnedSymbolAppService = seedOwnedSymbolAppService;
        }
        
        [HttpPost]
        [Route("nft-collections-migrate")]
        public async Task CollectionMigrateAsync(CollectionMigrateInput input)
        {
            await _nftCollectionAppService.CollectionMigrateAsync(input);
        }

        [HttpGet]
        [Route("nft-collections")]
        public Task<PagedResultDto<NFTCollectionIndexDto>> GetNFTCollectionsAsync(GetNFTCollectionsInput input)
        {
            return _nftCollectionAppService.GetNFTCollectionsAsync(input);
        }
        
        [HttpGet]
        [Route("search-nft-collections")]
        public Task<PagedResultDto<SearchNFTCollectionsDto>> SearchNFTCollectionsAsync(SearchNFTCollectionsInput input)
        {
            return _nftCollectionAppService.SearchNFTCollectionsAsync(input);
        }
        
        [HttpGet]
        [Route("recommended-collections")]
        public Task<List<RecommendedNFTCollectionsDto>> GetRecommendedNFTCcollectionsAsync()
        {
            return _nftCollectionAppService.GetRecommendedNFTCollectionsAsync();
        }
        
        [HttpGet]
        [Route("nft-collection/{id}")]
        public Task<NFTCollectionIndexDto> GetNFTCollectionAsync(string id)
        {
            return _nftCollectionAppService.GetNFTCollectionAsync(id);
        }
        
        [HttpGet]
        [Route("nft-infos-user-profile")]
        public Task<PagedResultDto<UserProfileNFTInfoIndexDto>> GetNFTInfosForUserProfileAsync(GetNFTInfosProfileInput input)
        {
            return _nftAppService.GetNFTInfosForUserProfileAsync(input);
        }

        [HttpPost]
        [Route("composite-nft-infos")]
        public Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetCompositeNFTInfosAsync(GetCompositeNFTInfosInput input)
        {
            return _nftAppService.GetCompositeNFTInfosAsync(input);
        }
        
        [HttpPost]
        [Route("collection-activities")]
        public Task<PagedResultDto<CollectionActivitiesDto>> GetCollectionActivitiesAsync(GetCollectionActivitiesInput input)
        {
            return _nftAppService.GetCollectionActivitiesAsync(input);
        }

        [HttpGet]
        [Route("seed-owned-symbols")]
        public Task<PagedResultDto<SeedSymbolIndexDto>> GetSeedNFTSymbolsAsync(GetSeedOwnedSymbols input)
        {
            return _seedOwnedSymbolAppService.GetSeedOwnedSymbolsAsync(input);
        }

        [HttpGet]
        [Route("nft-info")]
        public Task<NFTInfoIndexDto> GetNFTInfoAsync(GetNFTInfoInput input)
        {
            return _nftAppService.GetNFTInfoAsync(input);
        }
        
        [HttpGet]
        [Route("nft-for-sale")]
        public Task<NFTForSaleDto> GetNFTForSaleAsync(GetNFTForSaleInput input)
        {
            return _nftAppService.GetNFTForSaleAsync(input);
        }

        [HttpPost]
        [Route("nft-infos")]
        public Task CreateNFTAsync(CreateNFTInput input)
        {
            return _nftAppService.CreateNFTInfoExtensionAsync(new CreateNFTExtensionInput
            {
                ChainId = input.ChainId,
                TransactionId = input.TransactionId,
                Symbol = input.Symbol,
                Description = input.Description,
                ExternalLink = input.ExternalLink,
                PreviewImage = input.PreviewImage,
                File = input.File,
                CoverImageUrl = input.CoverImageUrl,
            });
        }


        [HttpPost]
        [Route("nft-collections")]
        [Authorize]
        public async Task CreateCollectionAsync(CreateCollectionInput input)
        {
            await _nftCollectionAppService.CreateCollectionExtensionAsync(new CreateCollectionExtensionInput
            {
                ChainId = input.FromChainId,
                TransactionId = input.TransactionId,
                Symbol = input.Symbol,
                Description = input.Description,
                ExternalLink = input.ExternalLink,
                LogoImage = input.LogoImage,
                FeaturedImage = input.FeaturedImage,
                TokenName = input.TokenName
            });
        }
        
        [HttpGet]
        [Route("activities")]
        public Task<PagedResultDto<NFTActivityDto>> GetListAsync(GetActivitiesInput input)
        {
            return _nftActivityAppService.GetListAsync(input);
        }
        
        [HttpGet]
        [Route("nft-info-owners")]
        public Task<NFTOwnerDto> GetNFTOwners(GetNFTOwnersInput input)
        {
            return _nftAppService.GetNFTOwnersAsync(input);
        }
    }
}