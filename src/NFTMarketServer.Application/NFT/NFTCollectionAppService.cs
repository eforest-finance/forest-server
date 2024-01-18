using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Common;
using NFTMarketServer.Grains;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Options;
using NFTMarketServer.Users;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT
{
    [RemoteService(IsEnabled = false)]
    public class NFTCollectionAppService : NFTMarketServerAppService, INFTCollectionAppService
    {
        private readonly IUserAppService _userAppService;
        private readonly INFTCollectionProvider _nftCollectionProvider;
        private readonly IObjectMapper _objectMapper;
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<NFTInfoAppService> _logger;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly INFTCollectionChangeService _nftCollectionChangeService;
        private readonly INFTCollectionExtensionProvider _collectionExtensionProvider;
        private readonly IOptionsMonitor<RecommendedCollectionsOptions> _optionsMonitor;
        private readonly IOptionsMonitor<NFTImageUrlOptions> _nftImageUrlOptionsMonitor;
        private const string BiztypeCreateCollectionExtensionAsync = "CreateCollectionExtensionAsync";
        
        public NFTCollectionAppService(
            IClusterClient clusterClient,
            IUserAppService userAppService,
            INFTCollectionProvider nftCollectionProvider,
            IObjectMapper objectMapper,
            ILogger<NFTInfoAppService> logger,
            INFTCollectionExtensionProvider collectionExtensionProvider,
            IDistributedEventBus distributedEventBus,
            INFTCollectionChangeService nftCollectionChangeService,
            IOptionsMonitor<RecommendedCollectionsOptions> optionsMonitor, 
            IOptionsMonitor<NFTImageUrlOptions> nftImageUrlOptionsMonitor)
        {
            _userAppService = userAppService;
            _nftCollectionProvider = nftCollectionProvider;
            _objectMapper = objectMapper;
            _clusterClient = clusterClient;
            _logger = logger;
            _collectionExtensionProvider = collectionExtensionProvider;
            _distributedEventBus = distributedEventBus;
            _nftCollectionChangeService = nftCollectionChangeService;
            _optionsMonitor = optionsMonitor;
            _nftImageUrlOptionsMonitor = nftImageUrlOptionsMonitor;
        }

        public async Task<PagedResultDto<NFTCollectionIndexDto>> GetNFTCollectionsAsync(GetNFTCollectionsInput input)
        {
            if (input.SkipCount < 0) return buildInitNFTCollectionIndexDto();
            var nftCollectionIndexs =
                await _nftCollectionProvider.GetNFTCollectionsIndexAsync(input.SkipCount,
                    input.MaxResultCount, input.Address);
            if (nftCollectionIndexs == null) return buildInitNFTCollectionIndexDto();

            var totalCount = nftCollectionIndexs.TotalRecordCount;
            if (nftCollectionIndexs.IndexerNftCollections == null)
            {
                return buildInitNFTCollectionIndexDto();
            }

            var addresses = nftCollectionIndexs.IndexerNftCollections.Select(o => o.CreatorAddress).Distinct().ToList();
            var accounts = await _userAppService.GetAccountsAsync(addresses);

            List<NFTCollectionIndexDto> nftCollectionIndexDtos = new List<NFTCollectionIndexDto>();
            
            var nftCollectionExtensionIds = nftCollectionIndexs.IndexerNftCollections.Select(o => o.Id).ToList();
            var nftCollectionExtensions =
                await _collectionExtensionProvider.GetNFTCollectionExtensionsAsync(nftCollectionExtensionIds);
            
            foreach (IndexerNFTCollection nft in nftCollectionIndexs.IndexerNftCollections)
            {
                nftCollectionIndexDtos.Add(MapForNFTCollection(nft, accounts, nftCollectionExtensions));
            }

            return new PagedResultDto<NFTCollectionIndexDto>
            {
                Items = nftCollectionIndexDtos,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResultDto<SearchNFTCollectionsDto>> SearchNFTCollectionsAsync(SearchNFTCollectionsInput input)
        {
            if (input.SkipCount < 0)
            {
                return buildInitSearchNFTCollectionsDto();
            }

            var tuple = await _collectionExtensionProvider.GetNFTCollectionExtensionAsync(input);

            var extensionList = tuple.Item2;
            if (extensionList.IsNullOrEmpty())
            {
                return buildInitSearchNFTCollectionsDto();
            }

            var ids = extensionList.Select(r => r.Id).ToList();
            var collectionDictionary = await _nftCollectionProvider.GetNFTCollectionIndexByIdsAsync(ids);

            var resultList = extensionList
                .Select(index => MapForSearchNftCollectionsDto(index, collectionDictionary))
                .ToList();
            return new PagedResultDto<SearchNFTCollectionsDto>
            {
                Items = resultList,
                TotalCount = tuple.Item1
            };
        }

        public async Task<List<RecommendedNFTCollectionsDto>> GetRecommendedNFTCollectionsAsync()
        {
            var recommendedCollectionsOptions = _optionsMonitor.CurrentValue;
            if (recommendedCollectionsOptions.RecommendedCollections.IsNullOrEmpty())
            {
                return new List<RecommendedNFTCollectionsDto>();
            }

            var ids = recommendedCollectionsOptions.RecommendedCollections
                .Select(r => r.id).ToList();

            var collectionDictionary = await _nftCollectionProvider.GetNFTCollectionIndexByIdsAsync(ids);

            return recommendedCollectionsOptions.RecommendedCollections
                .Select(r => MapForRecommendedNftCollectionsDto(r.id, collectionDictionary))
                .Where(dto => dto != null)
                .ToList();
        }

        private PagedResultDto<NFTCollectionIndexDto> buildInitNFTCollectionIndexDto()
        {
            return new PagedResultDto<NFTCollectionIndexDto>
            {
                Items = new List<NFTCollectionIndexDto>(),
                TotalCount = 0
            };
        }
        
        private PagedResultDto<SearchNFTCollectionsDto> buildInitSearchNFTCollectionsDto()
        {
            return new PagedResultDto<SearchNFTCollectionsDto>
            {
                Items = new List<SearchNFTCollectionsDto>(),
                TotalCount = 0
            };
        }

        public async Task<NFTCollectionIndexDto> GetNFTCollectionAsync(string id)
        {
            if (id.IsNullOrEmpty())
            {
                return null;
            }

            var nftCollectionIndex = await _nftCollectionProvider.GetNFTCollectionIndexAsync(id);
            if (nftCollectionIndex == null) return null;
            
            var nftCollectionExtensionIds = new List<string>{nftCollectionIndex.Id};
            var nftCollectionExtensions =
                await _collectionExtensionProvider.GetNFTCollectionExtensionsAsync(nftCollectionExtensionIds);
            var accountMap = await _userAppService.GetAccountsAsync(new List<string>() { nftCollectionIndex.Creator });
            return MapForNFTCollection(nftCollectionIndex, accountMap, nftCollectionExtensions);
        }

        private NFTCollectionIndexDto MapForNFTCollection(IndexerNFTCollection index,
            Dictionary<string, AccountDto> accounts,
            Dictionary<string, NFTCollectionExtensionIndex> collectionExtensionIndexs)
        {
            var collection = _objectMapper.Map<IndexerNFTCollection, NFTCollectionIndexDto>(index);
            if (collectionExtensionIndexs != null
                && collectionExtensionIndexs.ContainsKey(index.Id)
                && collectionExtensionIndexs[index.Id] != null)
            {
                _objectMapper.Map(collectionExtensionIndexs[index.Id], collection);
            }
            if (accounts != null && accounts.ContainsKey(index.CreatorAddress))
            {
                collection.Creator = accounts[index.CreatorAddress]?.WithChainIdAddress(index.ChainId);
            }
            collection.Metadata = index.ExternalInfoDictionary
                .Select(item => new MetadataDto { Key = item.Key, Value = item.Value }).ToList();

            var imageUrl = GetNftImageUrl(index.Symbol, index.ExternalInfoDictionary);
            if (imageUrl.IsNullOrEmpty())
            {
                return collection;
            }
            collection.LogoImage ??= imageUrl;
            collection.FeaturedImage ??= imageUrl;
            return collection;
        }

        private SearchNFTCollectionsDto MapForSearchNftCollectionsDto(NFTCollectionExtensionIndex index, 
            Dictionary<string, IndexerNFTCollection> collectionInfos)
        {
            var searchNftCollectionsDto =
                _objectMapper.Map<NFTCollectionExtensionIndex, SearchNFTCollectionsDto>(index);
            
            if (collectionInfos != null && collectionInfos.TryGetValue(index.Id, out var info))
            { 
                searchNftCollectionsDto.LogoImage ??= GetNftImageUrl(info.Symbol, info.ExternalInfoDictionary);
            }
            
            return searchNftCollectionsDto;
        }

        private RecommendedNFTCollectionsDto MapForRecommendedNftCollectionsDto(string id,
            Dictionary<string, IndexerNFTCollection> collectionInfos)
        {
            RecommendedNFTCollectionsDto recommendedNftCollectionsDto = null;
            if (collectionInfos != null && collectionInfos.TryGetValue(id, out var info))
            {
                recommendedNftCollectionsDto = _objectMapper.Map<IndexerNFTCollection, RecommendedNFTCollectionsDto>(info);

                recommendedNftCollectionsDto.LogoImage ??= GetNftImageUrl(info.Symbol, info.ExternalInfoDictionary);
            }

            return recommendedNftCollectionsDto;
        }

        public async Task CreateCollectionExtensionAsync(CreateCollectionExtensionInput input)
        {
            String id = GrainIdHelper.GenerateGrainId(input.ChainId, input.Symbol);
            var extension = new NftCollectionExtensionGrainDto
            {
                Id = id,
                ChainId = input.ChainId,
                NFTSymbol = input.Symbol,
                Description = input.Description,
                TransactionId = input.TransactionId,
                ExternalLink = input.ExternalLink,
                LogoImage = input.LogoImage,
                FeaturedImage = input.FeaturedImage,
                TokenName = input.TokenName,
                CreateTime = DateTime.UtcNow
            };
            var nftCollectionExtensionGrain = _clusterClient.GetGrain<INFTCollectionExtensionGrain>(id);
            var resultDto = await nftCollectionExtensionGrain.CreateNftCollectionExtensionAsync(extension);
            if (!resultDto.Success)
            {
                _logger.LogError("CreateCollectionExtension  fail, CollectionExtension id: {id}.", id);
                return;
            }
            var grainDto = resultDto.Data;
            await _distributedEventBus.PublishAsync(_objectMapper.Map<NftCollectionExtensionGrainDto, NFTCollectionExtraEto>(grainDto));
            //manually start statistics once
            await _nftCollectionChangeService.HandleItemsChangesAsync(grainDto.ChainId,
                new List<IndexerNFTCollectionChange> { new(grainDto.ChainId, grainDto.NFTSymbol, -1) });
            await _nftCollectionChangeService.HandlePriceChangesAsync(grainDto.ChainId,
                new List<IndexerNFTCollectionPriceChange> { new(grainDto.ChainId, grainDto.NFTSymbol, -1) }, 0L,
                BiztypeCreateCollectionExtensionAsync);
        }

        public async Task CollectionMigrateAsync(CollectionMigrateInput input)
        {
            foreach (var inputId in input.Ids)
            {
                var collectionExtension = await _collectionExtensionProvider.GetNFTCollectionExtensionAsync(inputId);
                var collection = await _nftCollectionProvider.GetNFTCollectionIndexAsync(inputId);

                if (collection == null)
                {
                    return;
                }
                collectionExtension.Id = inputId;
                collectionExtension.CreateTime = collection.CreateTime;
                collectionExtension.TokenName ??= collection.TokenName;
                collectionExtension.ChainId ??= collection.ChainId;
                collectionExtension.NFTSymbol = collection.Symbol;
                collectionExtension.Description ??=collection.Description;
                collectionExtension.FeaturedImage ??=collection.FeaturedImage;
                collectionExtension.LogoImage ??= GetNftImageUrl(collection.Symbol, collection.ExternalInfoDictionary);
                
                var id = GrainIdHelper.GenerateGrainId(collectionExtension.ChainId, collectionExtension.NFTSymbol);
                var grainDto = _objectMapper.Map<NFTCollectionExtensionIndex, NftCollectionExtensionGrainDto>(collectionExtension);

                var nftCollectionExtensionGrain = _clusterClient.GetGrain<INFTCollectionExtensionGrain>(id);
                var resultDto = await nftCollectionExtensionGrain.CreateNftCollectionExtensionAsync(grainDto);
                if (!resultDto.Success)
                {
                    _logger.LogError("CreateCollectionExtension  fail, CollectionExtension id: {id}.", id);
                    return;
                }
                await _distributedEventBus.PublishAsync(_objectMapper.Map<NftCollectionExtensionGrainDto, NFTCollectionExtraEto>(grainDto));
                //manually start statistics once
                await _nftCollectionChangeService.HandleItemsChangesAsync(grainDto.ChainId,
                    new List<IndexerNFTCollectionChange> { new(grainDto.ChainId, grainDto.NFTSymbol, -1) });
                await _nftCollectionChangeService.HandlePriceChangesAsync(grainDto.ChainId,
                    new List<IndexerNFTCollectionPriceChange> { new(grainDto.ChainId, grainDto.NFTSymbol, -1) }, 0L,BiztypeCreateCollectionExtensionAsync);
                
            }
        }

        private string GetNftImageUrl(string symbol, List<IndexerExternalInfoDictionary> externalInfo)
        {
            return NFTHelper.GetNftImageUrl(externalInfo, 
                () => _nftImageUrlOptionsMonitor.CurrentValue.GetImageUrl(symbol));
        }
    }
}