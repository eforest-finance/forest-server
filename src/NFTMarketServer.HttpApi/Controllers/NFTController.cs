using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Ai;
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
        private readonly IAiAppService _aiAppService;


        public NFTController(
            IAiAppService aiAppService,
            INFTInfoAppService nftAppService,
            INFTCollectionAppService nftCollectionAppService,
            INFTActivityAppService nftActivityAppService, 
            ISeedOwnedSymbolAppService seedOwnedSymbolAppService)
        {
            _nftAppService = nftAppService;
            _nftCollectionAppService = nftCollectionAppService;
            _nftActivityAppService = nftActivityAppService;
            _seedOwnedSymbolAppService = seedOwnedSymbolAppService;
            _aiAppService = aiAppService;
        }

        [HttpPost]
        [Route("create-ai-arts")]
        [Authorize]
        public async Task<PagedResultDto<CreateAiArtDto>> CreateAiArtAsync(CreateAiArtInput input)
        {
            return await _aiAppService.CreateAiArtAsync(input);
        }
        
        [HttpPost]
        [Route("create-ai-arts/v2")]
        [Authorize]
        public async Task<CreateAiResultDto> CreateAiArtAsyncV2(CreateAiArtInput input)
        {
            return await _aiAppService.CreateAiArtAsyncV2(input);
        }
        
        [HttpGet]
        [Route("create-ai-arts-retry")]
        [Authorize]
        public async Task<PagedResultDto<CreateAiArtDto>> CreateAiArtRetryAsync(CreateAiArtRetryInput input)
        {
            return await _aiAppService.CreateAiArtRetryAsync(input);
        }
        
        [HttpPost]
        [Route("ai-arts-fail")]
        [Authorize]
        public async Task<PagedResultDto<AiArtFailDto>> CreateAiArtFailAsync(QueryAiArtFailInput input)
        {
            return await _aiAppService.QueryAiArtFailAsync(input);
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
        
        [HttpPost]
        [Route("collected-collection-activities")]
        public Task<PagedResultDto<CollectedCollectionActivitiesDto>> GetCollectedCollectionActivitiesAsync(GetCollectedCollectionActivitiesInput input)
        {
            return _nftAppService.GetCollectedCollectionActivitiesAsync(input);
        }
        
        [HttpGet]
        [Route("hot-nft-infos")]
        public Task<PagedResultDto<HotNFTInfoDto>> GetHotNFTInfosAsync()
        {
            return _nftAppService.GetHotNFTInfosAsync();
        }
        
        [HttpGet]
        [Route("all-seed-owned-symbols")]
        public Task<PagedResultDto<SeedSymbolIndexDto>> GetAllSeedNFTSymbolsAsync(GetAllSeedOwnedSymbols input)
        {
            return _seedOwnedSymbolAppService.GetAllSeedOwnedSymbolsAsync(input);
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
        [Route("batch-nft-infos")]
        public async Task BatchCreateNFTAsync(BatchCreateNFTInput batchCreateNFTInput)
        {
            var tasks = batchCreateNFTInput.NFTList.Select(input => _nftAppService.CreateNFTInfoExtensionAsync(new CreateNFTExtensionInput
                {
                    ChainId = input.ChainId,
                    TransactionId = input.TransactionId,
                    Symbol = input.Symbol,
                    Description = input.Description,
                    ExternalLink = input.ExternalLink,
                    PreviewImage = input.PreviewImage,
                    File = input.File,
                    CoverImageUrl = input.CoverImageUrl,
                }))
                .ToList();
            await Task.WhenAll(tasks);
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
        
        [HttpGet]
        [Route("activities-time")]
        public Task<PagedResultDto<NFTActivityDto>> GetActivityListAsync(GetActivitiesInput input)
        {
            return _nftAppService.GetActivityListAsync(input);
        }
        
        [HttpGet]
        [Route("ai-arts")]
        public async Task<PagedResultDto<CreateAiArtDto>> GETAIArts(GetAIArtsInput input)
        {
            return await _aiAppService.GetAiArtsAsync(input);
        }
        
        [HttpPost]
        [Route("ai-arts")]
        [Authorize]
        public async Task<ResultDto<string>> UseAIArts(UseAIArtsInput input)
        {
            return await _aiAppService.UseAIArtsAsync(input);
        }
        
        [HttpGet]
        [Route("ai-prompts")]
        public  ResultDto<string> GETAIPrompts()
        {
            return _aiAppService.GETAIPrompts();
        }
        
        [HttpGet]
        [Route("nft-collections/myhold")]
        public Task<PagedResultDto<SearchNFTCollectionsDto>> GetMyHoldNFTCollectionsAsync(GetMyHoldNFTCollectionsInput input)
        {
            return _nftCollectionAppService.GetMyHoldNFTCollectionsAsync(input);
        }
        
        [HttpPost]
        [Route("nft-infos-user-profile/myhold")]
        public Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetMyHoldNFTInfosAsync(GetMyHoldNFTInfosInput input)
        {
            return _nftAppService.GetMyHoldNFTInfosAsync(input);
        }
        
        [HttpPost]
        [Route("nft-infos-user-profile/mycreated")]
        public Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetMyCreateNFTInfosAsync(GetMyCreateNFTInfosInput input)
        {
            return _nftAppService.GetMyCreatedNFTInfosAsync(input);
        }
        
    }
}