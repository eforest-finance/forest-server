using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
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
        private readonly NFTMarketServer.Users.Provider.IUserBalanceProvider _userBalanceProvider;
        private const int MaxQueryBalanceCount = 10;


        
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
            IOptionsMonitor<NFTImageUrlOptions> nftImageUrlOptionsMonitor,
            NFTMarketServer.Users.Provider.IUserBalanceProvider userBalanceProvider)
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
            _userBalanceProvider = userBalanceProvider;
        }

        public async Task<PagedResultDto<NFTCollectionIndexDto>> GetNFTCollectionsAsync(GetNFTCollectionsInput input)
        {
            if (input.SkipCount < 0) return BuildInitNFTCollectionIndexDto();
            var nftCollectionIndexs =
                await _nftCollectionProvider.GetNFTCollectionsIndexAsync(input.SkipCount,
                    input.MaxResultCount, input.AddressList.IsNullOrEmpty()?new List<string>{input.Address}:input.AddressList);
            if (nftCollectionIndexs == null) return BuildInitNFTCollectionIndexDto();

            var totalCount = nftCollectionIndexs.TotalRecordCount;
            if (nftCollectionIndexs.IndexerNftCollections == null)
            {
                return BuildInitNFTCollectionIndexDto();
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

        public async Task<PagedResultDto<SearchCollectionsFloorPriceDto>> SearchCollectionsFloorPriceAsync(
            SearchCollectionsFloorPriceInput input)
        {
            if (input.ChainId.IsNullOrEmpty() || input.CollectionSymbolList.IsNullOrEmpty())
            {
                return BuildInitSearchCollectionsFloorPriceDto();
            }

            var tuple = await _collectionExtensionProvider.GetNFTCollectionExtensionAsync(input);

            var extensionList = tuple.Item2;
            if (extensionList.IsNullOrEmpty())
            {
                return BuildInitSearchCollectionsFloorPriceDto();
            }
            var resultList = extensionList
                .Select(index => MapForSearchNftCollectionsFloorPriceDto(index))
                .ToList();
            return new PagedResultDto<SearchCollectionsFloorPriceDto>
            {
                Items = resultList,
                TotalCount = tuple.Item1
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
                .Select(index => MapForSearchNftCollectionsDto(index, collectionDictionary, input.DateRangeType))
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

        private PagedResultDto<NFTCollectionIndexDto> BuildInitNFTCollectionIndexDto()
        {
            return new PagedResultDto<NFTCollectionIndexDto>
            {
                Items = new List<NFTCollectionIndexDto>(),
                TotalCount = 0
            };
        }
        
        private PagedResultDto<SearchCollectionsFloorPriceDto> BuildInitSearchCollectionsFloorPriceDto()
        {
            return new PagedResultDto<SearchCollectionsFloorPriceDto>
            {
                Items = new List<SearchCollectionsFloorPriceDto>(),
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

            collection.IsMainChainCreateNFT = NFTHelper.GetIsMainChainCreateNFT(index.ExternalInfoDictionary);
            
            var imageUrl = GetNftImageUrl(index.Symbol, index.ExternalInfoDictionary);
            if (imageUrl.IsNullOrEmpty())
            {
                collection.LogoImage = FTHelper.BuildIpfsUrl(collection.LogoImage);
                return collection;
            }
            collection.LogoImage ??= imageUrl;
            collection.FeaturedImage ??= imageUrl;
            return collection;
        }

        private SearchNFTCollectionsDto MapForSearchNftCollectionsDto(NFTCollectionExtensionIndex index, 
            Dictionary<string, IndexerNFTCollection> collectionInfos, DateRangeType dateRangeType)
        {
            var searchNftCollectionsDto =
                _objectMapper.Map<NFTCollectionExtensionIndex, SearchNFTCollectionsDto>(index);

            if (DateRangeType.byday == dateRangeType)
            {
                searchNftCollectionsDto.FloorChange = index.CurrentDayFloorChange;
                searchNftCollectionsDto.VolumeTotal = index.CurrentDayVolumeTotal;
                searchNftCollectionsDto.VolumeTotalChange = index.CurrentDayVolumeTotalChange;
                searchNftCollectionsDto.SalesTotal = index.CurrentDaySalesTotal;
            }
            else
            {
                searchNftCollectionsDto.FloorChange = index.CurrentWeekFloorChange;
                searchNftCollectionsDto.VolumeTotal = index.CurrentWeekVolumeTotal;
                searchNftCollectionsDto.VolumeTotalChange = index.CurrentWeekVolumeTotalChange;
                searchNftCollectionsDto.SalesTotal = index.CurrentWeekSalesTotal;
            }
            
            
            if (collectionInfos != null && collectionInfos.TryGetValue(index.Id, out var info))
            { 
                searchNftCollectionsDto.LogoImage ??= GetNftImageUrl(info.Symbol, info.ExternalInfoDictionary);
                searchNftCollectionsDto.LogoImage = FTHelper.BuildIpfsUrl(searchNftCollectionsDto.LogoImage);
            }
            
            return searchNftCollectionsDto;
        }
        
        private SearchCollectionsFloorPriceDto MapForSearchNftCollectionsFloorPriceDto(NFTCollectionExtensionIndex index)
        {
            return _objectMapper.Map<NFTCollectionExtensionIndex, SearchCollectionsFloorPriceDto>(index);
        }

        private RecommendedNFTCollectionsDto MapForRecommendedNftCollectionsDto(string id,
            Dictionary<string, IndexerNFTCollection> collectionInfos)
        {
            RecommendedNFTCollectionsDto recommendedNftCollectionsDto = null;
            if (collectionInfos != null && collectionInfos.TryGetValue(id, out var info))
            {
                recommendedNftCollectionsDto = _objectMapper.Map<IndexerNFTCollection, RecommendedNFTCollectionsDto>(info);

                recommendedNftCollectionsDto.LogoImage ??= GetNftImageUrl(info.Symbol, info.ExternalInfoDictionary);
                recommendedNftCollectionsDto.LogoImage = FTHelper.BuildIpfsUrl(recommendedNftCollectionsDto.LogoImage);
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

        public async Task<PagedResultDto<SearchNFTCollectionsDto>> GetMyHoldNFTCollectionsAsync(GetMyHoldNFTCollectionsInput input)
        {
            var queryUserBalanceIndexInput = new QueryUserBalanceIndexInput()
            {
                Address = input.Address,
                QueryType = input.QueryType,
                KeyWord = input.KeyWord,
                SkipCount = CommonConstant.IntZero
            };
            var userBalanceList = await _userBalanceProvider.GetValidUserBalanceInfosAsync(queryUserBalanceIndexInput);
            if (userBalanceList.IsNullOrEmpty())
            {
                return new PagedResultDto<SearchNFTCollectionsDto>()
                {
                    TotalCount = CommonConstant.IntZero,
                    Items = new List<SearchNFTCollectionsDto>()
                };
            }

            var collectionIds = userBalanceList.Select(i => i.CollectionId).Distinct().ToList();
            _logger.LogDebug("GetMyHoldNFTCollectionsAsync for debug query userBalance collectionIdsCount:{A} collectionIds:{}", collectionIds.Count, JsonConvert.SerializeObject(collectionIds));

            var collectionDictionary = await _nftCollectionProvider.GetNFTCollectionIndexByIdsAsync(collectionIds);
            _logger.LogDebug("GetMyHoldNFTCollectionsAsync for debug query userBalance collectionDictionary:{A}", JsonConvert.SerializeObject(collectionDictionary));

            if (collectionDictionary == null || collectionDictionary.Count == CommonConstant.IntZero)
            {
                return new PagedResultDto<SearchNFTCollectionsDto>()
                {
                    TotalCount = CommonConstant.IntZero,
                    Items = new List<SearchNFTCollectionsDto>()
                };
            }
            //query nft extensionAsync
            var tuple = await _collectionExtensionProvider.GetNFTCollectionExtensionByIdsAsync(new SearchCollectionsFloorPriceInput()
            {
                CollectionIdList = collectionIds
            });
            var extensionList = tuple.Item2;

            List<SearchNFTCollectionsDto> nftCollectionIndexDtos = new List<SearchNFTCollectionsDto>();
            foreach (var indexerNFTCollection in collectionDictionary.Values)
            {
                nftCollectionIndexDtos.Add(MapNFTCollectionInfo(indexerNFTCollection, extensionList));
            }

            if (input.SkipCount >= collectionDictionary.Count)
            {
                return new PagedResultDto<SearchNFTCollectionsDto>()
                {
                    TotalCount = collectionDictionary.Count,
                    Items = new List<SearchNFTCollectionsDto>()
                };
            }

            if (input.MaxResultCount > (collectionDictionary.Count - input.SkipCount))
            {
                input.MaxResultCount = (collectionDictionary.Count - input.SkipCount);
            }

            return new PagedResultDto<SearchNFTCollectionsDto>()
            {
                TotalCount = collectionDictionary.Count,
                Items = nftCollectionIndexDtos.GetRange(input.SkipCount,input.MaxResultCount)
            };
        }

        private SearchNFTCollectionsDto MapNFTCollectionInfo(IndexerNFTCollection nftCollection,
            List<NFTCollectionExtensionIndex> extensionIndices)
        {
            var dto = new SearchNFTCollectionsDto();
            dto.LogoImage = nftCollection.LogoImage;
            dto.TokenName = nftCollection.TokenName;
            dto.ChainId = nftCollection.ChainId;
            dto.Symbol = nftCollection.Symbol;
            dto.Id = nftCollection.Id;
            if (!extensionIndices.IsNullOrEmpty())
            {
                var extension = extensionIndices.FirstOrDefault(i => i.Id.Equals(nftCollection.Id));

                if (extension != null)
                {
                    dto.FloorPrice = extension.FloorPrice;
                    dto.FloorPriceSymbol = extension.FloorPriceSymbol;
                    dto.ItemTotal = extension.ItemTotal;
                }
            }

            return dto;
        }


        private string GetNftImageUrl(string symbol, List<IndexerExternalInfoDictionary> externalInfo)
        {
            return NFTHelper.GetNftImageUrl(externalInfo, 
                () => _nftImageUrlOptionsMonitor.CurrentValue.GetImageUrl(symbol));
        }
    }
}