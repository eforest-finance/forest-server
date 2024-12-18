using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Contracts.HandleException;
using NFTMarketServer.Entities;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.Helper;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Options;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Index;
using Orleans;
using Orleans.Runtime;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT
{
    [RemoteService(IsEnabled = false)]
    public class NFTInfoAppService : NFTMarketServerAppService, INFTInfoAppService
    {
        private readonly ITokenAppService _tokenAppService;
        private readonly IUserAppService _userAppService;
        private readonly IClusterClient _clusterClient;
        private readonly INFTInfoProvider _nftInfoProvider;
        private readonly INFTCollectionProvider _nftCollectionProvider;
        private readonly ILogger<NFTInfoAppService> _logger;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IObjectMapper _objectMapper;
        private readonly INFTInfoExtensionProvider _nftInfoExtensionProvider;

        private readonly INESTRepository<NFTInfoIndex, string> _nftInfoIndexRepository;
        private readonly INESTRepository<NFTInfoNewIndex, string> _nftInfoNewIndexRepository;
        private readonly ISeedSymbolSyncedProvider _seedSymbolSyncedProvider;
        private readonly INFTInfoSyncedProvider _nftInfoSyncedProvider;
        private readonly INFTInfoNewSyncedProvider _nftInfoNewSyncedProvider;
        private readonly INFTOfferProvider _nftOfferProvider;
        private readonly INFTListingProvider _nftListingProvider;
        private readonly INFTCollectionExtensionProvider _nftCollectionExtensionProvider;
        private readonly INFTDealInfoProvider _nftDealInfoProvider;
        private readonly IInscriptionProvider _inscriptionProvider;
        private readonly NFTCollectionAppService _nftCollectionAppService;
        private readonly IDistributedCache<string> _distributedCacheForHeight;
        private readonly IGraphQLProvider _graphQlProvider;
        private readonly INFTTraitProvider _inftTraitProvider;
        private readonly INFTActivityAppService _nftActivityAppService;
        private readonly ISeedAppService _seedAppService;
        private readonly IRarityProvider _rarityProvider;
        private readonly ICompositeNFTProvider _compositeNFTProvider;
        private readonly INFTOfferAppService _nftOfferAppService;

        private readonly IOptionsMonitor<ResetNFTSyncHeightExpireMinutesOptions>
            _resetNFTSyncHeightExpireMinutesOptionsMonitor;

        private readonly IOptionsMonitor<ChoiceNFTInfoNewFlagOptions>
            _choiceNFTInfoNewFlagOptionsMonitor;

        private readonly IOptionsMonitor<CollectionActivityNFTLimitOptions>
            _collectionActivityNFTLimitOptionsMonitor;

        private readonly IOptionsMonitor<RecommendHotNFTOptions> _recommendHotNFTOptionsMonitor;
        private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;

        private readonly IUserBalanceProvider _userBalanceProvider;
        private readonly ISchrodingerInfoProvider _schrodingerInfoProvider;
        private readonly string _defaultMainChain = "AELF";
        private readonly NFTMarketServer.Users.Provider.IUserBalanceProvider _userBalanceIndexProvider;

        private readonly IOptionsMonitor<FuzzySearchOptions>
            _fuzzySearchOptionsMonitor;

        public NFTInfoAppService(
            ITokenAppService tokenAppService, IUserAppService userAppService,
            INFTCollectionProvider nftCollectionProvider,
            INFTInfoProvider nftInfoProvider,
            IClusterClient clusterClient,
            ILogger<NFTInfoAppService> logger, IDistributedEventBus distributedEventBus,
            IObjectMapper objectMapper, INFTInfoExtensionProvider nftInfoExtensionProvider,
            INESTRepository<NFTInfoIndex, string> nftInfoIndexRepository,
            INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
            ISeedSymbolSyncedProvider seedSymbolSyncedProvider,
            INFTInfoSyncedProvider nftInfoSyncedProvider,
            INFTInfoNewSyncedProvider nftInfoNewSyncedProvider,
            INFTOfferProvider nftOfferProvider,
            INFTListingProvider nftListingProvider,
            INFTDealInfoProvider nftDealInfoProvider,
            IInscriptionProvider inscriptionProvider,
            INFTCollectionExtensionProvider nftCollectionExtensionProvider,
            NFTCollectionAppService nftCollectionAppService,
            IDistributedCache<string> distributedCacheForHeight,
            IGraphQLProvider graphQlProvider,
            IOptionsMonitor<ResetNFTSyncHeightExpireMinutesOptions> resetNFTSyncHeightExpireMinutesOptionsMonitor,
            IOptionsMonitor<CollectionActivityNFTLimitOptions> collectionActivityNFTLimitOptionsMonitor,
            INFTTraitProvider inftTraitProvider,
            IUserBalanceProvider userBalanceProvider,
            INFTActivityAppService nftActivityAppService,
            ISeedAppService seedAppService,
            IOptionsMonitor<RecommendHotNFTOptions> recommendHotNFTOptionsMonitor,
            IOptionsMonitor<ChoiceNFTInfoNewFlagOptions> choiceNFTInfoNewFlagOptionsMonitor,
            ISchrodingerInfoProvider schrodingerInfoProvider,
            IRarityProvider rarityProvider,
            IOptionsMonitor<ChainOptions> chainOptionsMonitor,
            ICompositeNFTProvider compositeNFTProvider,
            NFTMarketServer.Users.Provider.IUserBalanceProvider userBalanceIndexProvider,
            IOptionsMonitor<FuzzySearchOptions> fuzzySearchOptionsMonitor,
            INFTOfferAppService nftOfferAppService)
        {
            _tokenAppService = tokenAppService;
            _userAppService = userAppService;
            _clusterClient = clusterClient;
            _nftInfoProvider = nftInfoProvider;
            _nftCollectionProvider = nftCollectionProvider;
            _logger = logger;
            _distributedEventBus = distributedEventBus;
            _objectMapper = objectMapper;
            _nftInfoExtensionProvider = nftInfoExtensionProvider;
            _nftInfoIndexRepository = nftInfoIndexRepository;
            _seedSymbolSyncedProvider = seedSymbolSyncedProvider;
            _nftInfoSyncedProvider = nftInfoSyncedProvider;
            _nftInfoNewSyncedProvider = nftInfoNewSyncedProvider;
            _nftInfoNewIndexRepository = nftInfoNewIndexRepository;
            _nftOfferProvider = nftOfferProvider;
            _nftListingProvider = nftListingProvider;
            _nftDealInfoProvider = nftDealInfoProvider;
            _nftCollectionExtensionProvider = nftCollectionExtensionProvider;
            _inscriptionProvider = inscriptionProvider;
            _nftCollectionAppService = nftCollectionAppService;
            _distributedCacheForHeight = distributedCacheForHeight;
            _resetNFTSyncHeightExpireMinutesOptionsMonitor = resetNFTSyncHeightExpireMinutesOptionsMonitor;
            _choiceNFTInfoNewFlagOptionsMonitor = choiceNFTInfoNewFlagOptionsMonitor;
            _collectionActivityNFTLimitOptionsMonitor = collectionActivityNFTLimitOptionsMonitor;
            _graphQlProvider = graphQlProvider;
            _inftTraitProvider = inftTraitProvider;
            _userBalanceProvider = userBalanceProvider;
            _nftActivityAppService = nftActivityAppService;
            _seedAppService = seedAppService;
            _recommendHotNFTOptionsMonitor = recommendHotNFTOptionsMonitor;
            _schrodingerInfoProvider = schrodingerInfoProvider;
            _chainOptionsMonitor = chainOptionsMonitor;
            _rarityProvider = rarityProvider;
            _userBalanceIndexProvider = userBalanceIndexProvider;
            _compositeNFTProvider = compositeNFTProvider;
            _fuzzySearchOptionsMonitor = fuzzySearchOptionsMonitor;
            _nftOfferAppService = nftOfferAppService;
        }

        public async Task<PagedResultDto<UserProfileNFTInfoIndexDto>> GetNFTInfosForUserProfileAsync(
            GetNFTInfosProfileInput input)
        {
            //query nft infos

            var nftInfos = await _nftInfoNewSyncedProvider.GetNFTInfosUserProfileAsync(input);

            //query seed infos
            var seedInfos = await _seedSymbolSyncedProvider.GetSeedInfosUserProfileAsync(input);
            var totalRecordCount = nftInfos.TotalRecordCount + seedInfos.TotalRecordCount;
            if (totalRecordCount == 0)
            {
                return PagedResultWrapper<UserProfileNFTInfoIndexDto>.Initialize();
            }

            //convert seed info to nft info
            var resultNftInfos = seedInfos.IndexerSeedInfoList.Select(MapForIndexerSeedInfo).ToList();
            if (nftInfos.IndexerNftInfos != null)
            {
                resultNftInfos.AddRange(nftInfos.IndexerNftInfos.Select(MapForIndexerNFTInfo));
            }

            var loginAddress = await _userAppService.TryGetCurrentUserAddressAsync();
            var isInRarityWhiteList = await _rarityProvider.CheckAddressIsInWhiteListAsync(loginAddress);
            var result = await BuildNFTInfoIndexListAsync(input.Address, resultNftInfos, isInRarityWhiteList);
            return new PagedResultDto<UserProfileNFTInfoIndexDto>
            {
                Items = result.Select(item =>
                {
                    var newItem = _objectMapper.Map<NFTInfoIndexDto, UserProfileNFTInfoIndexDto>(item);
                    newItem.PreviewImage = FTHelper.BuildIpfsUrl(newItem.PreviewImage);
                    return newItem;
                }).ToList(),

                TotalCount = (long)(totalRecordCount == null ? CommonConstant.IntZero : totalRecordCount)
            };
        }

        [ExceptionHandler(typeof(Exception),
            Message = "NFTInfoAppService.GetCompositeNFTInfosAsync Something is wrong",
            LogOnly = true,
            TargetType = typeof(ExceptionHandlingService),
            MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
            LogTargets = new[] { "input" }
        )]
        public virtual async Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetCompositeNFTInfosAsync(
            GetCompositeNFTInfosInput input)
        {
            var result = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();
            var fuzzySearchSwitch = _fuzzySearchOptionsMonitor.CurrentValue.FuzzySearchSwitch;
            input.FuzzySearchSwitch = fuzzySearchSwitch;
            input.PageFrom = PageFromEnum.NFTLIST;

            if (input.CollectionType.Equals(CommonConstant.CollectionTypeSeed))
            {
                var seedResult = await _seedSymbolSyncedProvider.GetSeedBriefInfosAsync(input);
                //to get max offers
                var maxOfferDict = await GetMaxOfferInfosAsync(seedResult.Item2.Select(info => info.Id).ToList());

                var accountDtoDict =
                    await _userAppService.GetAccountsAsync(seedResult.Item2.Select(info => info.RealOwner).ToList());

                result = new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = seedResult.Item1,
                    Items = seedResult.Item2.Select(item => MapForSeedBriefInfoDto(item, maxOfferDict, accountDtoDict))
                        .ToList()
                };
            }

            if (input.CollectionType.Equals(CommonConstant.CollectionTypeNFT))
            {
                var nftResult = await _nftInfoNewSyncedProvider.GetNFTBriefInfosAsync(input);

                var maxOfferDict = await GetMaxOfferInfosAsync(nftResult.Item2.Select(info => info.Id).ToList());

                var accountDtoDict =
                    await _userAppService.GetAccountsAsync(nftResult.Item2.Select(info => info.RealOwner).ToList());

                result = new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = nftResult.Item1,
                    Items = nftResult.Item2.Select(item => MapForNftBriefInfoDto(item, maxOfferDict, accountDtoDict))
                        .ToList()
                };
            }

            var collectionInfo = await _nftCollectionProvider.GetNFTCollectionIndexAsync(input.CollectionId);

            if (collectionInfo == null || collectionInfo.TokenName.IsNullOrEmpty())
            {
                return await MapForCompositeNftInfoIndexDtoPage(result);
            }

            var checkInput = new SearchNFTCollectionsInput()
            {
                TokenName = collectionInfo.TokenName
            };
            var collectionResult = await _nftCollectionAppService.SearchNFTCollectionsAsync(checkInput);
            var searchNftCollectionsDto = collectionResult?.Items?.FirstOrDefault();

            if (searchNftCollectionsDto == null ||
                searchNftCollectionsDto.ItemTotal == result.TotalCount)
            {
                return await MapForCompositeNftInfoIndexDtoPage(result);
            }

            _logger.LogDebug(
                "searchNftCollectionsDto.ItemTotal is not equals  result.TotalCount collectionId ={A} ",
                input.CollectionId);

            var resetNFTSyncHeightFlagCacheKey = CommonConstant.ResetNFTNewSyncHeightFlagCacheKey;

            var resetSyncHeightFlag =
                await _distributedCacheForHeight.GetAsync(resetNFTSyncHeightFlagCacheKey);
            _logger.LogDebug(
                "GetCompositeNFTInfosAsync origin {ResetSyncHeightFlag} {resetNftSyncHeightExpireMinutes}",
                resetSyncHeightFlag,
                _resetNFTSyncHeightExpireMinutesOptionsMonitor?.CurrentValue.ResetNFTSyncHeightExpireMinutes);
            if (resetSyncHeightFlag.IsNullOrEmpty())
            {
                var temValue = _resetNFTSyncHeightExpireMinutesOptionsMonitor?.CurrentValue?
                    .ResetNFTSyncHeightExpireMinutes ?? CommonConstant.IntZero;
                var resetNftSyncHeightExpireMinutes =
                    temValue != CommonConstant.IntZero ? temValue : CommonConstant.CacheExpirationMinutes;

                await _distributedCacheForHeight.SetAsync(resetNFTSyncHeightFlagCacheKey,
                    resetNFTSyncHeightFlagCacheKey, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(resetNftSyncHeightExpireMinutes)
                    });
            }

            return await MapForCompositeNftInfoIndexDtoPage(result);
        }

        public async Task<PagedResultDto<CollectionActivitiesDto>> GetCollectionActivitiesAsync(
            GetCollectionActivitiesInput input)
        {
            var result = PagedResultWrapper<CollectionActivitiesDto>.Initialize();

            var collectionActivityNFTLimit =
                _collectionActivityNFTLimitOptionsMonitor?.CurrentValue?.CollectionActivityNFTLimit ??
                CommonConstant.CollectionActivityNFTLimit;

            var basicInfoDic = new Dictionary<string, CollectionActivityBasicDto>();

            var collectionInfo = await _nftCollectionProvider.GetNFTCollectionIndexAsync(input.CollectionId);

            if (collectionInfo == null)
            {
                return result;
            }

            if (input.CollectionType.Equals(CommonConstant.CollectionTypeSeed))
            {
                var nftResult =
                    await _seedSymbolSyncedProvider.GetSeedBriefInfosAsync(input, collectionActivityNFTLimit);

                if (nftResult == null || nftResult.Item2.IsNullOrEmpty())
                {
                    return result;
                }

                basicInfoDic = nftResult.Item2.Select(item => new CollectionActivityBasicDto
                {
                    NFTInfoId = item.Id,
                    NFTTokenName = item.TokenName,
                    Image = item.SeedImage,
                    ChainId = item.ChainId
                }).ToList().ToDictionary(e => e.NFTInfoId, e => e);
                ;
            }

            if (input.CollectionType.Equals(CommonConstant.CollectionTypeNFT))
            {
                var nftResult =
                    await _nftInfoNewSyncedProvider.GetNFTBriefInfosAsync(input, collectionActivityNFTLimit);

                if (nftResult == null || nftResult.Item2.IsNullOrEmpty())
                {
                    return result;
                }

                basicInfoDic = nftResult.Item2.Select(item =>
                {
                    var collectionActivityBasicDto = new CollectionActivityBasicDto
                    {
                        Image = FTHelper.BuildIpfsUrl(item.ImageUrl)
                    };
                    _objectMapper.Map(item, collectionActivityBasicDto);
                    return collectionActivityBasicDto;
                }).ToList().ToDictionary(e => e.NFTInfoId, e => e);
            }

            var getCollectionActivityListInput = new GetCollectionActivityListInput
            {
                CollectionId = input.CollectionId,
                BizIdList = basicInfoDic.Keys.ToList(),
                Types = input.Type,
                SkipCount = input.SkipCount,
                MaxResultCount = input.MaxResultCount
            };
            var nftActivityDtoPage =
                await _nftActivityAppService.GetCollectionActivityListAsync(getCollectionActivityListInput);

            if (nftActivityDtoPage == null || nftActivityDtoPage.Items.IsNullOrEmpty())
            {
                return result;
            }

            var loginAddress = await _userAppService.TryGetCurrentUserAddressAsync();
            var isInRarityWhiteList = await _rarityProvider.CheckAddressIsInWhiteListAsync(loginAddress);

            var collectionActivitiesDtoList = nftActivityDtoPage.Items.ToList().Select(item =>
            {
                var itemNew = _objectMapper.Map<NFTActivityDto, CollectionActivitiesDto>(item);
                itemNew.NFTCollectionName = collectionInfo.TokenName;
                basicInfoDic.TryGetValue(item.NFTInfoId, out var collectionActivityBasicDto);

                if (collectionActivityBasicDto != null && isInRarityWhiteList)
                {
                    _objectMapper.Map(collectionActivityBasicDto, itemNew);
                }

                if (itemNew.PriceToken == null)
                {
                    itemNew.PriceToken = new TokenDto()
                    {
                        ChainId = collectionActivityBasicDto?.ChainId
                    };
                }

                if (itemNew.PriceToken.ChainId.IsNullOrEmpty())
                {
                    itemNew.PriceToken.ChainId = collectionActivityBasicDto?.ChainId;
                }

                return itemNew;
            }).ToList();

            result = new PagedResultDto<CollectionActivitiesDto>()
            {
                TotalCount = nftActivityDtoPage.TotalCount,
                Items = collectionActivitiesDtoList
            };

            return result;
        }

        public async Task<PagedResultDto<CollectedCollectionActivitiesDto>> GetCollectedCollectionActivitiesAsync(
            GetCollectedCollectionActivitiesInput input)
        {
            input.Address = FullAddressHelper.ToShortAddress(input.Address);
            var result = PagedResultWrapper<CollectedCollectionActivitiesDto>.Initialize();

            var nftActivityDtoPage = new PagedResultDto<CollectedCollectionActivitiesDto>();

            var nftInfoIds = new List<string>();
            if (!input.SearchParam.IsNullOrEmpty() || !input.CollectionIdList.IsNullOrEmpty())
            {
                int skip = CommonConstant.IntZero;
                var compositeNFTDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(input.CollectionIdList,
                    input.SearchParam, skip, CommonConstant.IntOneThousand);
                nftInfoIds = compositeNFTDic?.Keys.ToList();
                //while (nftInfoIds.Count >= CommonConstant.IntOneThousand) todo v2
                // if (nftInfoIds.Count >= CommonConstant.IntOneThousand)
                // {
                //     skip += CommonConstant.IntOneThousand;
                //     compositeNFTDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(input.CollectionIdList,
                //         input.SearchParam, skip, CommonConstant.IntOneThousand);
                //     var infoIds = compositeNFTDic?.Keys.ToList();
                //     if (infoIds.IsNullOrEmpty())
                //     {
                //         //break; todo v2
                //         return result;
                //     }
                //
                //     nftInfoIds.AddRange(infoIds);
                // }

                if (nftInfoIds.IsNullOrEmpty())
                {
                    return result;
                }
            }

            _logger.LogInformation("QueryCompositeNFTInfoAsync nftInfoIds{A} ", nftInfoIds.Count);

            nftActivityDtoPage =
                await _nftActivityAppService.GetCollectedCollectionActivitiesAsync(input, nftInfoIds);
            _logger.LogInformation("QueryCompositeNFTInfoAsync nftActivityDtoPage TotalCount {A} itemsCount:{B}",
                nftActivityDtoPage.TotalCount, nftActivityDtoPage.Items.Count);

            if (nftActivityDtoPage == null || nftActivityDtoPage.Items.IsNullOrEmpty())
            {
                return result;
            }

            var collectionActivitiesDtoList = nftActivityDtoPage.Items.ToList().Select(item =>
                _objectMapper.Map<NFTActivityDto, CollectedCollectionActivitiesDto>(item)).ToList();

            result = new PagedResultDto<CollectedCollectionActivitiesDto>()
            {
                TotalCount = nftActivityDtoPage.TotalCount,
                Items = collectionActivitiesDtoList
            };

            return result;
        }
        [ExceptionHandler(typeof(Exception),
            Message = "NFTInfoAppService.GetHotNFTInfosAsync query or set from cache error",
            LogOnly = true,
            TargetType = typeof(ExceptionHandlingService),
            MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow)
        )]
        public virtual async Task<PagedResultDto<HotNFTInfoDto>> GetHotNFTInfosAsync()
        {
            var resultList = new List<IndexerNFTInfo>();
            if (_recommendHotNFTOptionsMonitor.CurrentValue.HotNFTCacheIsOn)
            {
                var cacheResult = await _distributedCacheForHeight.GetAsync(CommonConstant.HotNFTInfosCacheKey);
                if (!cacheResult.IsNullOrEmpty())
                {
                    return JsonConvert.DeserializeObject<PagedResultDto<HotNFTInfoDto>>(cacheResult);
                }
            }

            var recommendHotNFTList = _recommendHotNFTOptionsMonitor.CurrentValue.RecommendHotNFTList;

            var recommendHotNFTIds = recommendHotNFTList?.Select(item => item.NFTInfoId).ToList();
            var recommendNFTPage = await _nftInfoNewSyncedProvider.GetRecommendHotNFTInfosAsync(recommendHotNFTIds);

            if (!recommendHotNFTList.IsNullOrEmpty()
                && recommendNFTPage != null
                && !recommendNFTPage.Item2.IsNullOrEmpty())
            {
                var recommendNFTDic = recommendNFTPage.Item2.ToDictionary(item => item.Id, item => item);

                foreach (var recommendHotNFT in recommendHotNFTList)
                {
                    if (recommendNFTDic.TryGetValue(recommendHotNFT.NFTInfoId, out var value))
                    {
                        resultList.Add(value);
                    }
                }
            }

            var realHotNFTSize = Math.Max(CommonConstant.IntTen - resultList.Count, 0);
            var realHotNFTPageInfo =
                await _nftInfoNewSyncedProvider.GetHotNFTInfosAsync(recommendHotNFTIds, realHotNFTSize);

            if (realHotNFTPageInfo != null && !realHotNFTPageInfo.Item2.IsNullOrEmpty())
            {
                resultList.AddRange(realHotNFTPageInfo.Item2);
            }

            var address = await _userAppService.TryGetCurrentUserAddressAsync();
            _logger.LogDebug("HotNFT TryGetCurrentUserAddressAsync address={A}", address);
            var isInRarityWhiteList = await _rarityProvider.CheckAddressIsInWhiteListAsync(address);
            var result = MapForHotNFTInfoDtoPage(resultList, recommendHotNFTList, isInRarityWhiteList);

            var pageResult = new PagedResultDto<HotNFTInfoDto>()
            {
                TotalCount = result.Count,
                Items = result
            };
            if (!_recommendHotNFTOptionsMonitor.CurrentValue.HotNFTCacheIsOn)
            {
                return pageResult;
            }

            await _distributedCacheForHeight.SetAsync(CommonConstant.HotNFTInfosCacheKey,
                JsonConvert.SerializeObject(pageResult), new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration =
                        DateTimeOffset.Now.AddMinutes(
                            _recommendHotNFTOptionsMonitor.CurrentValue.HotNFTCacheMinutes)
                });

            return pageResult;
        }

        private List<HotNFTInfoDto> MapForHotNFTInfoDtoPage(
            List<IndexerNFTInfo> nftInfoList, IEnumerable<RecommendHotNFT> recommendHotNFTList,
            bool isInRarityWhiteList)
        {
            if (nftInfoList.IsNullOrEmpty())
            {
                new Empty();
            }

            foreach (var info in nftInfoList)
            {
                if (info.PreviewImage.IsNullOrEmpty())
                {
                    var nftImageUrl =
                        info?.ExternalInfoDictionary?.FirstOrDefault(o => o.Key == CommonConstant.MetadataImageUrlKey);
                    info.PreviewImage = nftImageUrl?.Value;
                }

                if (info.PreviewImage.IsNullOrEmpty())
                {
                    var nftImageUri =
                        info?.ExternalInfoDictionary?.FirstOrDefault(o => o.Key == CommonConstant.MetadataImageUriKey);
                    info.PreviewImage = nftImageUri?.Value;
                }

                if (info.PreviewImage.IsNullOrEmpty())
                {
                    info.PreviewImage = info.ImageUrl;
                }

                info.PreviewImage = FTHelper.BuildIpfsUrl(info?.PreviewImage);
            }

            var result = nftInfoList.Select(item =>
                {
                    var tem = _objectMapper.Map<IndexerNFTInfo, HotNFTInfoDto>(item);
                    if (!isInRarityWhiteList)
                    {
                        tem.Rank = CommonConstant.IntZero;
                        tem.Level = "";
                        tem.Grade = "";
                        tem.Star = "";
                        tem.Rarity = "";
                        tem.Describe = "";
                    }

                    return tem;
                }
            ).ToList();

            var recommendHotNFTDic = recommendHotNFTList?.ToDictionary(item => item.NFTInfoId, item => item);

            if (recommendHotNFTDic == null || result.IsNullOrEmpty())
            {
                return result;
            }

            foreach (var item in result)
            {
                if (recommendHotNFTDic.TryGetValue(item.Id, out var value))
                {
                    item.Link = value.Link;
                }
            }

            return result;
        }

        private async Task<PagedResultDto<CompositeNFTInfoIndexDto>> MapForCompositeNftInfoIndexDtoPage(
            PagedResultDto<CompositeNFTInfoIndexDto> pageInfo)
        {
            if (pageInfo == null || pageInfo.TotalCount == 0)
            {
                return pageInfo;
            }

            var ids = pageInfo.Items.Select(dto => dto.Id).ToList();
            var nftInfoExtensions = await _nftInfoExtensionProvider.GetNFTInfoExtensionsAsync(ids);

            foreach (var item in pageInfo.Items)
            {
                if (!item.Id.IsNullOrWhiteSpace() && nftInfoExtensions.TryGetValue(item.Id, out var extension))
                {
                    _objectMapper.Map(extension, item);
                    item.PreviewImage ??= extension.PreviewImage;
                }

                item.PreviewImage = FTHelper.BuildIpfsUrl(item.PreviewImage);

                if (item.fileExtension.IsNullOrWhiteSpace())
                {
                    item.fileExtension = CommonConstant.FILE_TYPE_IMAGE;
                }
            }

            return pageInfo;
        }

        public async Task<SymbolInfoDto> GetSymbolInfoAsync(GetSymbolInfoInput input)
        {
            if (input == null || input.Symbol.IsNullOrWhiteSpace())
            {
                return new SymbolInfoDto
                {
                    Exist = false
                };
            }

            var nftCollectionSymbol = await _nftInfoProvider.GetNFTCollectionSymbolAsync(input.Symbol);
            if (nftCollectionSymbol != null && !nftCollectionSymbol.Symbol.IsNullOrWhiteSpace())
            {
                return new SymbolInfoDto
                {
                    Exist = true
                };
            }

            var nftSymbol = await _nftInfoProvider.GetNFTSymbolAsync(input.Symbol);
            if (nftSymbol != null && !nftSymbol.Symbol.IsNullOrWhiteSpace())
            {
                return new SymbolInfoDto
                {
                    Exist = true
                };
            }

            return new SymbolInfoDto
            {
                Exist = false
            };
        }

        private async Task<decimal> GetPriceElfFromUSDAsync(decimal usdPrice, string symbol)
        {
            var marketData = await _tokenAppService.GetTokenMarketDataAsync(symbol, null);
            if (marketData == null)
            {
                Log.Error("GetPriceElfFromUSDAsync query fail:result is null");
                return usdPrice;
            }

            if (marketData.Price <= 0)
            {
                Log.Error("GetPriceElfFromUSDAsync query fail:result<0");
                return usdPrice;
            }

            return usdPrice / marketData.Price;
        }
        [ExceptionHandler(typeof(Exception),
            Message = "NFTInfoAppService.GetNFTInfoAsync Query inscriptionInfo from graphQl error tick",
            LogOnly = true,
            TargetType = typeof(ExceptionHandlingService),
            MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
            LogTargets = new []{"input"}

        )]
        public virtual async Task<NFTInfoIndexDto> GetNFTInfoAsync(GetNFTInfoInput input)
        {
            if (input == null || input.Id.IsNullOrWhiteSpace())
            {
                return null;
            }

            var nftInfoIndex = await _nftInfoNewSyncedProvider.GetNFTInfoIndexAsync(input.Id);

            if (nftInfoIndex == null)
            {
                return null;
            }

            //convert listing price
            nftInfoIndex = MapForIndexerNFTInfo(nftInfoIndex);
            var addresses = GetAddresses(nftInfoIndex);
            var accounts = await _userAppService.GetAccountsAsync(addresses);
            var nftExtensions =
                await _nftInfoExtensionProvider.GetNFTInfoExtensionsAsync(new List<string> { nftInfoIndex.Id });
            var collectionInfos = await _nftCollectionProvider.GetNFTCollectionIndexByIdsAsync(
                new List<string> { nftInfoIndex.CollectionId });

            var loginAddress = await _userAppService.TryGetCurrentUserAddressAsync();
            var nftBalance = await GetNFTBalanceAsync(loginAddress, nftInfoIndex.Id);
            _logger.LogDebug("nftinfo TryGetCurrentUserAddressAsync {A} nftBalance:{B}", loginAddress, nftBalance);
            var isInRarityWhiteList = await _rarityProvider.CheckAddressIsInWhiteListAsync(loginAddress);
            var nftInfoIndexDto =
                MapForIndexerNFTInfos(nftInfoIndex, accounts, nftExtensions, collectionInfos, isInRarityWhiteList);
            //set default true
            var canBuyFlag = true;
            if (!input.Address.IsNullOrWhiteSpace())
            {
                canBuyFlag = await GetCanBuyFlagAsync(nftInfoIndex.ChainId, nftInfoIndex.Symbol, input.Address);
            }

            nftInfoIndexDto.BestOffer = await GetBestOfferAsync(nftInfoIndex.ChainId, input.Id, loginAddress);
            nftInfoIndexDto.NFTBalance = nftBalance;
            nftInfoIndexDto.CanBuyFlag = canBuyFlag;

            // seed create token info
            if (input.Id.Match(StringHelper.SeedIdPattern))
            {
                nftInfoIndexDto.CreateTokenInformation =
                    _objectMapper.Map<IndexerNFTInfo, CreateTokenInformation>(nftInfoIndex);
            }

            if (!SymbolHelper.CheckSymbolIsNoMainChainNFT(nftInfoIndex.Symbol, nftInfoIndex.ChainId))
            {
                return nftInfoIndexDto;
            }

            //build priceType and price info
            nftInfoIndexDto = await BuildShowPriceTypeAsync(input.Address, nftInfoIndex, nftInfoIndex.Symbol,
                nftInfoIndexDto);

            var kv = nftInfoIndexDto?.Metadata?.Where(item =>
                    item.Key.Equals(CommonConstant.MetadataSpecialInscriptionImageKey) ||
                    item.Key.Equals(CommonConstant.MetadataImageUriKey))
                .ToList()
                .FirstOrDefault();
            if (kv == null || string.IsNullOrEmpty(kv.Key))
            {
                return nftInfoIndexDto;
            }

            if (kv.Key.Equals(CommonConstant.MetadataSpecialInscriptionImageKey))
            {
                var tick = SymbolHelper.GainInscriptionInfoTick(nftInfoIndex.Symbol);
                var inscriptionInfoDto =
                    await _inscriptionProvider.GetIndexerInscriptionInfoAsync(nftInfoIndex.ChainId, tick);
                if (inscriptionInfoDto != null && inscriptionInfoDto.MintLimit == 0)
                {
                    inscriptionInfoDto.MintLimit = CommonConstant.DefaultValueNone;
                }

                nftInfoIndexDto.InscriptionInfo = inscriptionInfoDto;
            }
            else if (kv.Key.Equals(CommonConstant.MetadataImageUriKey))
            {
                var tickKv = nftInfoIndexDto?.Metadata?.Where(item =>
                        item.Key.Equals(CommonConstant.NFT_ExternalInfo_InscriptionDeploy_Key) ||
                        item.Key.Equals(CommonConstant.NFT_ExternalInfo_Inscription_Adopt_Key))
                    .ToList()
                    .FirstOrDefault();
                var inscriptionInfoDto = new InscriptionInfoDto();
                nftInfoIndexDto.InscriptionInfo = inscriptionInfoDto;
                if (tickKv == null || string.IsNullOrEmpty(tickKv.Key))
                {
                    return nftInfoIndexDto;
                }

                var tickDto = JsonConvert.DeserializeObject<TickDto>(tickKv.Value);
                if (tickDto == null)
                {
                    return nftInfoIndexDto;
                }

                inscriptionInfoDto.Tick = tickDto.Tick;
                inscriptionInfoDto.MintLimit = string.IsNullOrEmpty(tickDto.Lim)
                    ? CommonConstant.IntNegativeOne
                    : FTHelper.GetIntegerDivision(long.Parse(tickDto.Lim),
                        nftInfoIndex.Decimals);
            }

            return nftInfoIndexDto;
        }

        private async Task<NFTOfferDto> GetBestOfferAsync(string chainId, string nftInfoId, string address)
        {
            var query = new GetNFTOffersInput()
            {
                SkipCount = 0,
                MaxResultCount = 1,
                NFTInfoId = nftInfoId,
                ChainId = chainId,
                ExcludeAddress = address
            };
            //PagedResultDto<NFTOfferDto>
            var result = await _nftOfferAppService.GetNFTOffersAsync(query);
            if (result == null || result.TotalCount == 0 || result.Items.IsNullOrEmpty())
            {
                return null;
            }

            return result.Items.FirstOrDefault();
        }

        private async Task<decimal> GetNFTBalanceAsync(string address, string nftInfoId)
        {
            if (address.IsNullOrEmpty())
            {
                return 0;
            }

            var input = new QueryUserBalanceIndexInput()
            {
                Address = address,
                SkipCount = 0,
                MaxResultCount = 1,
                QueryType = QueryType.HOLDING,
                NFTInfoId = nftInfoId
            };
            var userBalanceList =
                await _userBalanceIndexProvider.GetUserBalancesByNFTInfoIdAsync(input);
            if (userBalanceList == null || userBalanceList.Item1 == 0 || userBalanceList.Item2.IsNullOrEmpty())
            {
                return 0;
            }

            var balance = userBalanceList.Item2.FirstOrDefault();
            var amount = balance == null ? 0 : (decimal)balance.Amount / (decimal)Math.Pow(10, balance.Decimals);
            return amount;
        }

        private NFTInfoIndexDto MapMinListingInfo(NFTInfoIndexDto nftInfoIndexDto, IndexerNFTListingInfo listingDto)
        {
            nftInfoIndexDto.ListingId = listingDto.Id;
            nftInfoIndexDto.ListingPrice = listingDto.Prices;
            nftInfoIndexDto.ListingAddress = listingDto.Owner;
            nftInfoIndexDto.ListingQuantity = listingDto.RealQuantity;
            nftInfoIndexDto.ListingEndTime = listingDto.ExpireTime;
            nftInfoIndexDto.LatestListingTime = listingDto.StartTime;
            return nftInfoIndexDto;
        }

        private async Task<NFTInfoIndexDto> BuildShowPriceTypeAsync(string address, IndexerNFTInfo indexerNFTInfo,
            string symbol,
            NFTInfoIndexDto nftInfoIndexDto)
        {
            var chainId = indexerNFTInfo.ChainId;
            var getMyNftListingsDto = new GetNFTListingsDto()
            {
                ChainId = chainId,
                Symbol = symbol,
                SkipCount = 0,
                MaxResultCount = 1
            };
            var allMinListingPage = await _nftListingProvider.GetNFTListingsAsync(getMyNftListingsDto);
            IndexerNFTListingInfo allMinListingDto = null;
            nftInfoIndexDto.ListingPrice = -1;
            nftInfoIndexDto.MaxOfferPrice = -1;
            if (allMinListingPage != null && allMinListingPage.TotalCount > 0)
            {
                allMinListingDto = allMinListingPage.Items[0];
            }

            if (allMinListingDto?.Prices != indexerNFTInfo.MinListingPrice)
            {
                await _distributedEventBus.PublishAsync(new NFTInfoResetEto
                {
                    NFTInfoId = indexerNFTInfo.Id,
                    ChainId = indexerNFTInfo.ChainId,
                    NFTType = NFTType.NFT
                });
            }

            var indexerNFTOffer = await _nftOfferProvider.GetMaxOfferInfoAsync(nftInfoIndexDto.Id);
            if (indexerNFTOffer != null && !indexerNFTOffer.Id.IsNullOrEmpty())
            {
                nftInfoIndexDto.MaxOfferPrice = indexerNFTOffer.Price;
            }

            //otherMinListing
            if (!address.IsNullOrEmpty())
            {
                var getOtherNftListingsDto = new GetNFTListingsDto()
                {
                    ChainId = chainId,
                    Symbol = symbol,
                    ExcludedAddress = address,
                    SkipCount = 0,
                    MaxResultCount = 1
                };
                var listingDto = await _nftListingProvider.GetNFTListingsAsync(getOtherNftListingsDto);
                if (listingDto != null && listingDto.TotalCount > 0)
                {
                    nftInfoIndexDto.ShowPriceType = ShowPriceType.OTHERMINLISTING.ToString();
                    return MapMinListingInfo(nftInfoIndexDto, listingDto.Items[0]);
                }
            }

            //allMinListing
            {
                if (allMinListingPage != null && allMinListingPage.TotalCount > 0)
                {
                    nftInfoIndexDto.ShowPriceType = ShowPriceType.MYMINLISTING.ToString();
                    return MapMinListingInfo(nftInfoIndexDto, allMinListingPage.Items[0]);
                }
            }

            //maxOffer
            {
                if (indexerNFTInfo?.OfferPrice != indexerNFTOffer?.Price)
                {
                    await _distributedEventBus.PublishAsync(new NFTInfoResetEto
                    {
                        NFTInfoId = indexerNFTInfo.Id,
                        ChainId = indexerNFTInfo.ChainId,
                        NFTType = NFTType.NFT
                    });
                }

                if (indexerNFTOffer != null && !indexerNFTOffer.Id.IsNullOrEmpty())
                {
                    nftInfoIndexDto.ShowPriceType = ShowPriceType.MAXOFFER.ToString();
                    nftInfoIndexDto.MaxOfferPrice = indexerNFTOffer.Price;
                    nftInfoIndexDto.MaxOfferEndTime = indexerNFTOffer.ExpireTime;
                    nftInfoIndexDto.MaxOfferToken = new TokenDto()
                    {
                        ChainId = indexerNFTOffer.PurchaseToken.ChainId,
                        Address = indexerNFTOffer.PurchaseToken.Address,
                        Symbol = indexerNFTOffer.PurchaseToken.Symbol,
                        Decimals = Convert.ToInt32(indexerNFTOffer.PurchaseToken.Decimals),
                    };
                    return nftInfoIndexDto;
                }
            }

            //latestDeal
            if (nftInfoIndexDto.LatestDealPrice != null && nftInfoIndexDto.LatestDealPrice > 0)
            {
                nftInfoIndexDto.ShowPriceType = ShowPriceType.LATESTDEAL.ToString();
                return nftInfoIndexDto;
            }

            nftInfoIndexDto.ShowPriceType = ShowPriceType.OTHER.ToString();
            return nftInfoIndexDto;
        }

        private async Task<List<NFTInfoIndexDto>> BuildNFTInfoIndexListAsync(string address,
            List<IndexerNFTInfo> nftInfos, bool isInRarityWhiteList)
        {
            var addresses = new List<string>();
            foreach (var info in nftInfos)
            {
                if (!info.Issuer.IsNullOrWhiteSpace())
                {
                    addresses.Add(info.Issuer);
                }
            }

            var accounts = await _userAppService.GetAccountsAsync(addresses);

            var nftExtensions =
                await _nftInfoExtensionProvider.GetNFTInfoExtensionsAsync(nftInfos
                    .Select(item => item.Id).ToList());

            var collectionInfos = await _nftCollectionProvider.GetNFTCollectionIndexByIdsAsync(
                nftInfos.Select(item => item.CollectionId).ToList());

            var result = nftInfos
                .Select(o => MapForIndexerNFTInfos(o, accounts, nftExtensions, collectionInfos, isInRarityWhiteList))
                .ToList();

            return result;
        }

        private NFTInfoIndexDto MapForIndexerNFTInfos(IndexerNFTInfo index,
            Dictionary<string, AccountDto> accounts,
            Dictionary<string, NFTInfoExtensionIndex> nftInfoExtensions,
            Dictionary<string, IndexerNFTCollection> nftCollections,
            bool isInRarityWhiteList)
        {
            var info = _objectMapper.Map<IndexerNFTInfo, NFTInfoIndexDto>(index);
            _logger.LogDebug("MapForIndexerNFTInfos {A} {B}", isInRarityWhiteList, JsonConvert.SerializeObject(info));
            if (!isInRarityWhiteList)
            {
                info.Rank = CommonConstant.IntZero;
                info.Level = "";
                info.Grade = "";
                info.Star = "";
                info.Rarity = "";
                info.Describe = "";
            }

            if (info.IssueChainId != 0)
            {
                info.IssueChainIdStr = ChainHelper.ConvertChainIdToBase58(info.IssueChainId);
            }

            if (!index.Issuer.IsNullOrWhiteSpace() && accounts.TryGetValue(index.Issuer, out var account))
            {
                info.Minter = account?.WithChainIdAddress(info.IssueChainIdStr ?? index.ChainId);
            }

            if (!index.Owner.IsNullOrWhiteSpace() && accounts.TryGetValue(index.Owner, out var ownerAccount))
            {
                info.Owner = ownerAccount?.WithChainIdAddress(index.ChainId);
            }

            if (!index.Id.IsNullOrWhiteSpace() && nftInfoExtensions.ContainsKey(index.Id))
            {
                _objectMapper.Map(nftInfoExtensions[index.Id], info);
                info.Uri = nftInfoExtensions[index.Id].ExternalLink;
            }

            if (nftCollections != null && nftCollections.ContainsKey(index.CollectionId)
                                       && nftCollections[index.CollectionId] != null)
            {
                info.NFTCollection =
                    _objectMapper.Map<IndexerNFTCollection, NFTCollectionIndexDto>(nftCollections[index.CollectionId]);
                info.NFTCollection.LogoImage = FTHelper.BuildIpfsUrl(info.NFTCollection.LogoImage);
                if (accounts != null && accounts.ContainsKey(nftCollections[index.CollectionId].CreatorAddress))
                {
                    info.NFTCollection.Creator = accounts[nftCollections[index.CollectionId].CreatorAddress]
                        ?.WithChainIdAddress(info.ChainId);
                }
            }

            if (index.ListingToken != null)
            {
                info.ListingToken = _objectMapper.Map<IndexerTokenInfo, TokenDto>(index.ListingToken);
            }

            if (index.LatestDealToken != null)
            {
                info.LatestDealToken = _objectMapper.Map<IndexerTokenInfo, TokenDto>(index.LatestDealToken);
            }

            info.NFTSymbol = index.Symbol;
            info.NFTTokenId = SymbolHelper.SubHyphenNumber(index.Symbol);
            info.TotalQuantity = index.Supply;
            if (index.ExternalInfoDictionary != null)
            {
                info.Metadata = index.ExternalInfoDictionary
                    .Select(kv => new MetadataDto { Key = kv.Key, Value = kv.Value }).ToList();
            }

            if (!index.TraitPairsDictionary.IsNullOrEmpty())
            {
                info.TraitPairsDictionary = index.TraitPairsDictionary
                    .Select(kv => new MetadataDto { Key = kv.Key, Value = kv.Value }).ToList();
            }

            info.Generation = index.Generation;

            if (info.PreviewImage.IsNullOrEmpty())
            {
                var nftImageUrl = info?.Metadata?.FirstOrDefault(o => o.Key == CommonConstant.MetadataImageUrlKey);
                info.PreviewImage = nftImageUrl?.Value;
            }

            if (info.PreviewImage.IsNullOrEmpty())
            {
                var nftImageUri = info?.Metadata?.FirstOrDefault(o => o.Key == CommonConstant.MetadataImageUriKey);
                info.PreviewImage = nftImageUri?.Value;
            }

            if (info.PreviewImage.IsNullOrEmpty())
            {
                info.PreviewImage = index.ImageUrl;
            }

            info.PreviewImage = FTHelper.BuildIpfsUrl(info?.PreviewImage);

            return info;
        }

        private IndexerNFTInfo MapForIndexerSeedInfo(IndexerSeedInfo index)
        {
            var nftInfo = _objectMapper.Map<IndexerSeedInfo, IndexerNFTInfo>(index);
            //set default chainId-SEED-0
            nftInfo.CollectionId = IdGenerateHelper.GetNFTCollectionId(index.ChainId, SymbolHelper.SEED_COLLECTION);
            nftInfo.OfListingPrice(index.HasListingFlag, index.MinListingPrice, index.ListingToken);
            return nftInfo;
        }

        private IndexerNFTInfo MapForIndexerNFTInfo(IndexerNFTInfo index)
        {
            index.OfListingPrice(index.HasListingFlag, index.MinListingPrice, index.ListingToken);
            return index;
        }

        private static List<string> GetAddresses(IndexerNFTInfo nftInfoIndex)
        {
            var addresses = new List<string>();
            if (!nftInfoIndex.Issuer.IsNullOrWhiteSpace())
            {
                addresses.Add(nftInfoIndex.Issuer);
            }

            if (nftInfoIndex.Owner.IsNullOrWhiteSpace())
            {
                return addresses;
            }

            addresses.Add(nftInfoIndex.Owner);
            return addresses;
        }

        private static bool PreCheckGetNFTInfosInput(GetNFTInfosInput input)
        {
            if (input == null)
            {
                Log.Error("NFTInfoAppService#GetNFTInfosAsync param input is null");
                return false;
            }

            return true;
        }

        [Authorize]
        [ExceptionHandler(typeof(Exception),
            Message = "NFTInfoAppService.CreateNFTInfoExtensionAsync Create NftInfoExtension exception, NftInfoExtension",
            LogOnly = true,
            TargetType = typeof(ExceptionHandlingService),
            MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
            LogTargets = new []{"input"}

        )]
        public virtual async Task CreateNFTInfoExtensionAsync(CreateNFTExtensionInput input)
        {
            string id = IdGenerateHelper.GetNftExtensionId(input.ChainId, input.Symbol);
            _logger.LogInformation("CreateNFTInfoExtensionAsync , id: {id}", id);
            var fileExtension = string.IsNullOrEmpty(input.File) ? null : input.File.Split(".").Last().ToLower();
            var extension = new NftInfoExtensionGrainDto()
            {
                Id = id,
                ChainId = input.ChainId,
                Description = input.Description,
                NFTSymbol = input.Symbol,
                TransactionId = input.TransactionId,
                ExternalLink = input.ExternalLink,
                PreviewImage = input.PreviewImage,
                File = input.File,
                FileExtension = fileExtension,
                CoverImageUrl = input.CoverImageUrl
            };
            var userGrain = _clusterClient.GetGrain<INftInfoExtensionGrain>(extension.Id);
            var result = await userGrain.CreateNftInfoExtensionAsync(extension);
            if (!result.Success)
            {
                _logger.LogError("Create NftInfoExtension fail, NftInfoExtension id: {id}.", extension.Id);
                return;
            }

            await _distributedEventBus.PublishAsync(
                _objectMapper.Map<NftInfoExtensionGrainDto, NFTInfoExtraEto>(result.Data));
        }

        public async Task AddOrUpdateNftInfoAsync(NFTInfoIndex nftInfo)
        {
            await _nftInfoIndexRepository.AddOrUpdateAsync(nftInfo);
        }

        public async Task AddOrUpdateNftInfoNewByIdAsync(string nftInfoId, string chainId)
        {
            _logger.LogDebug("AddOrUpdateNftInfoNewByIdAsync nftInfoId={A} chainId={B}", nftInfoId, chainId);
            if (string.IsNullOrEmpty(nftInfoId) || string.IsNullOrEmpty(chainId))
            {
                return;
            }

            if (SymbolHelper.CheckSymbolIsCommonNFTInfoId(nftInfoId))
            {
                await AddOrUpdateNftInfoNewAsync(null, nftInfoId, chainId);
            }
            else
            {
                await _seedAppService.UpdateSeedSymbolAsync(nftInfoId, chainId);
            }
        }

        public async Task AddOrUpdateNftInfoNewAsync(NFTInfoIndex fromNFTInfo, string nftInfoId,
            string chainId)
        {
            if (chainId.Equals(CommonConstant.MainChainId))
            {
                return;
            }

            if (fromNFTInfo == null)
            {
                fromNFTInfo = await _graphQlProvider.GetSyncNftInfoRecordAsync(nftInfoId, chainId);
            }

            if (fromNFTInfo == null)
            {
                _logger.LogError("AddOrUpdateNftInfoNewAsync fromNFTInfo and localNFTInfo are null! nftInfoI={A}",
                    nftInfoId);
                return;
            }

            var nftInfo = _objectMapper.Map<NFTInfoIndex, NFTInfoNewIndex>(fromNFTInfo);

            nftInfo.CountedFlag = FTHelper.IsGreaterThanEqualToOne(nftInfo.Supply, nftInfo.Decimals);


            nftInfo.Generation = CommonConstant.IntNegativeOne;
            if (nftInfo?.ExternalInfoDictionary != null && !nftInfo.ExternalInfoDictionary.IsNullOrEmpty())
            {
                nftInfo.TraitPairsDictionary = new List<ExternalInfoDictionary>();

                foreach (var item in nftInfo.ExternalInfoDictionary)
                {
                    if (item.Key == CommonConstant.NFT_ExternalInfo_InscriptionDeploy_Key)
                    {
                        var inscriptionDeploy = JsonConvert.DeserializeObject<InscriptionDeploy>(item.Value);
                        if (inscriptionDeploy != null && !inscriptionDeploy.Gen.IsNullOrEmpty())
                        {
                            nftInfo.Generation = int.Parse(inscriptionDeploy.Gen);
                            break;
                        }
                    }
                    else if (item.Key == CommonConstant.NFT_ExternalInfo_Inscription_Adopt_Key)
                    {
                        var inscriptionAdopt = JsonConvert.DeserializeObject<InscriptionAdop>(item.Value);
                        if (inscriptionAdopt != null && !inscriptionAdopt.Gen.IsNullOrEmpty())
                        {
                            nftInfo.Generation = int.Parse(inscriptionAdopt.Gen);
                            break;
                        }
                    }
                }

                foreach (var item in nftInfo.ExternalInfoDictionary)
                {
                    if (item.Key == CommonConstant.NFT_ExternalInfo_Attributes_Key)
                    {
                        var attributes = JsonConvert.DeserializeObject<List<AttributeDictionary>>(item.Value);
                        if (attributes.IsNullOrEmpty())
                        {
                            break;
                        }

                        nftInfo.TraitPairsDictionary.AddRange(
                            _objectMapper.Map<List<AttributeDictionary>, List<ExternalInfoDictionary>>(attributes));
                        break;
                    }
                }

                foreach (var item in nftInfo.ExternalInfoDictionary)
                {
                    if (item.Key == CommonConstant.NFT_ExternalInfo_Metadata_Key)
                    {
                        var metadata = JsonConvert.DeserializeObject<List<ExternalInfoDictionary>>(item.Value);
                        if (metadata.IsNullOrEmpty())
                        {
                            break;
                        }

                        nftInfo.TraitPairsDictionary.AddRange(metadata);
                        break;
                    }
                }
            }

            // add rarity info
            await BuildRarityInfo(nftInfo);

            await UpdateNFTOtherInfoAsync(nftInfo);
            await _inftTraitProvider.CheckAndUpdateTraitInfo(nftInfo);
        }

        private async Task BuildRarityInfo(NFTInfoNewIndex nftInfo)
        {
            _logger.LogInformation("BuildRarityInfo symbol ={A} gen={B}",
                nftInfo.Symbol, nftInfo.Generation);
            if (nftInfo.Generation == CommonConstant.Gen9)
            {
                var input = new GetCatListInput()
                {
                    ChainId = GetDefaultSideChainId(),
                    SkipCount = 0,
                    MaxResultCount = 1,
                    FilterSgr = true,
                    Keyword = nftInfo.Symbol
                };
                var schrodingerInfo = await _schrodingerInfoProvider.GetSchrodingerInfoAsync(input);
                _logger.LogInformation("BuildRarityInfo symbol ={A} gen={B} query:{C} input:{D}",
                    nftInfo.Symbol, nftInfo.Generation, JsonConvert.SerializeObject(schrodingerInfo),
                    JsonConvert.SerializeObject(input));
                if (schrodingerInfo.TotalCount != 0 && !schrodingerInfo.Data.IsNullOrEmpty())
                {
                    nftInfo.Rarity = schrodingerInfo.Data.First().Rarity;
                    nftInfo.Rank = schrodingerInfo.Data.First().Rank;
                    nftInfo.Level = schrodingerInfo.Data.First().Level;
                    nftInfo.Grade = schrodingerInfo.Data.First().Grade;
                    nftInfo.Star = schrodingerInfo.Data.First().Star;
                    nftInfo.Describe = GetDescribeByRank(schrodingerInfo.Data.First().Rank,
                        schrodingerInfo.Data.First().Level);
                }

                await _inftTraitProvider.CheckAndUpdateRarityInfo(nftInfo);
            }
        }

        private static string GetDescribeByRank(int rank, string level)
        {
            SchrodingerRankConsts.RankClassifyDictionary.TryGetValue(rank.ToString(), out var classify);
            SchrodingerLevelConsts.LevelDescribeDictionary.TryGetValue((level + "-" + classify), out var describe);
            return describe;
        }

        private async Task UpdateNFTOtherInfoAsync(NFTInfoNewIndex nftInfoNewIndex)
        {
            var getNftListingsDto = new GetNFTListingsDto
            {
                ChainId = nftInfoNewIndex.ChainId,
                Symbol = nftInfoNewIndex.Symbol,
                SkipCount = CommonConstant.IntZero,
                MaxResultCount = CommonConstant.IntOne
            };
            var listingDto = await _nftListingProvider.GetNFTListingsAsync(getNftListingsDto);

            if (listingDto != null && listingDto.TotalCount > CommonConstant.IntZero)
            {
                _logger.LogDebug(
                    "UpdateNFTOtherInfoAsync nftInfoNewIndex.Id={A} listingDto.Id={B} listingIsNull={C} listing price={D}",
                    nftInfoNewIndex.Id,
                    listingDto.Items[CommonConstant.IntZero]?.Id, listingDto.Items[CommonConstant.IntZero] == null,
                    listingDto.Items[CommonConstant.IntZero]?.Prices);
                UpdateMinListingInfo(nftInfoNewIndex, listingDto.Items[CommonConstant.IntZero]);
            }
            else
            {
                _logger.LogDebug("UpdateNFTOtherInfoAsync nftInfoNewIndex.Id={A} listingIsNull",
                    nftInfoNewIndex.Id);
                UpdateMinListingInfo(nftInfoNewIndex, null);
            }

            var indexerNFTOffer = await _nftOfferProvider.GetMaxOfferInfoAsync(nftInfoNewIndex.Id);
            _logger.LogDebug("UpdateNFTOtherInfoAsync nftInfoNewIndex.Id={A} indexerNFTOffer.Id={B} offerIsNull={C}",
                nftInfoNewIndex.Id,
                indexerNFTOffer?.Id, indexerNFTOffer == null);
            if (indexerNFTOffer != null && !indexerNFTOffer.Id.IsNullOrEmpty())
            {
                UpdateMaxOfferInfo(nftInfoNewIndex, indexerNFTOffer);
            }
            else
            {
                UpdateMaxOfferInfo(nftInfoNewIndex, null);
            }

            var balanceInfo = await _userBalanceProvider.GetNFTBalanceInfoAsync(nftInfoNewIndex.Id);
            if (balanceInfo != null)
            {
                nftInfoNewIndex.RealOwner = balanceInfo.Owner;
                nftInfoNewIndex.AllOwnerCount = balanceInfo.OwnerCount;
            }

            if (!nftInfoNewIndex.HasListingFlag)
            {
                nftInfoNewIndex.ListingPrice = CommonConstant.DefaultValueNone;
            }

            if (!nftInfoNewIndex.HasOfferFlag)
            {
                nftInfoNewIndex.MaxOfferPrice = CommonConstant.DefaultValueNone;
            }

            if (nftInfoNewIndex.LatestDealPrice == CommonConstant.IntZero)
            {
                nftInfoNewIndex.LatestDealPrice = CommonConstant.DefaultValueNone;
            }

            await _nftInfoNewIndexRepository.AddOrUpdateAsync(nftInfoNewIndex);
        }

        private bool UpdateMinListingInfo(NFTInfoNewIndex nftInfoIndex, IndexerNFTListingInfo listingDto)
        {
            _logger.LogDebug("UpdateMinListingInfo nftInfoIndexId={A} nftInfoIndex.ListingId={A2} listingDto={B}",
                nftInfoIndex.Id, nftInfoIndex.ListingId,
                JsonConvert.SerializeObject(listingDto));
            if (listingDto == null && nftInfoIndex.ListingId.IsNullOrEmpty())
            {
                if (nftInfoIndex.HasListingFlag)
                {
                    nftInfoIndex.HasListingFlag = false;
                    return true;
                }

                return false;
            }

            if (listingDto != null)
            {
                nftInfoIndex.ListingId = listingDto.Id;
                nftInfoIndex.ListingPrice = listingDto.Prices;

                nftInfoIndex.MinListingId = listingDto.Id;
                nftInfoIndex.MinListingPrice = listingDto.Prices;
                nftInfoIndex.MinListingExpireTime = listingDto.ExpireTime;

                nftInfoIndex.ListingAddress = listingDto?.Owner;
                nftInfoIndex.ListingQuantity = listingDto.RealQuantity;
                nftInfoIndex.ListingEndTime = listingDto.ExpireTime;
                nftInfoIndex.LatestListingTime = listingDto.StartTime;
                nftInfoIndex.ListingToken =
                    _objectMapper.Map<IndexerTokenInfo, TokenInfoIndex>(listingDto.PurchaseToken);
                nftInfoIndex.HasListingFlag = listingDto.Prices > CommonConstant.IntZero;
            }
            else
            {
                nftInfoIndex.ListingId = null;
                nftInfoIndex.ListingPrice = -1;

                nftInfoIndex.MinListingId = null;
                nftInfoIndex.MinListingPrice = -1;
                nftInfoIndex.MinListingExpireTime = DateTime.UtcNow;

                nftInfoIndex.ListingAddress = null;
                nftInfoIndex.ListingQuantity = 0;
                nftInfoIndex.ListingEndTime = DateTime.UtcNow;
                nftInfoIndex.LatestListingTime = DateTime.UtcNow;
                nftInfoIndex.ListingToken = null;
                nftInfoIndex.HasListingFlag = false;
            }

            return true;
        }

        private bool UpdateMaxOfferInfo(NFTInfoNewIndex nftInfoIndex, IndexerNFTOffer indexerNFTOffer)
        {
            if (indexerNFTOffer != null)
            {
                nftInfoIndex.MaxOfferId = indexerNFTOffer.Id;
                nftInfoIndex.MaxOfferPrice = indexerNFTOffer.Price;
                nftInfoIndex.MaxOfferExpireTime = indexerNFTOffer.ExpireTime;
                nftInfoIndex.OfferToken = new TokenInfoIndex
                {
                    ChainId = indexerNFTOffer.PurchaseToken.ChainId,
                    Symbol = indexerNFTOffer.PurchaseToken.Symbol,
                    Decimals = Convert.ToInt32(indexerNFTOffer.PurchaseToken.Decimals),
                    Prices = indexerNFTOffer.Price
                };
                nftInfoIndex.HasOfferFlag = nftInfoIndex.MaxOfferPrice > CommonConstant.IntZero;
            }
            else
            {
                nftInfoIndex.MaxOfferId = null;
                nftInfoIndex.MaxOfferPrice = -1;
                nftInfoIndex.MaxOfferExpireTime = DateTime.UtcNow;
                nftInfoIndex.OfferToken = null;
                nftInfoIndex.HasOfferFlag = false;
            }

            return true;
        }

        public async Task<NFTForSaleDto> GetNFTForSaleAsync(GetNFTForSaleInput input)
        {
            var nftInfoIndex = await _nftInfoNewSyncedProvider.GetNFTInfoIndexAsync(input.Id);

            if (nftInfoIndex == null)
            {
                _logger.LogInformation("The Nft Info with id {id} does not exist.", input.Id);
                return null;
            }

            var collectionId = IdGenerateHelper.GetNFTCollectionId(nftInfoIndex.ChainId, nftInfoIndex.CollectionSymbol);
            var collectionExtension =
                await _nftCollectionExtensionProvider.GetNFTCollectionExtensionAsync(collectionId);
            if (collectionExtension == null)
            {
                _logger.LogInformation("The Collection Extension with id {id} does not exist.", collectionId);
                return null;
            }

            var lastDealInfo = await GetLastDealAsync(nftInfoIndex.ChainId, nftInfoIndex.Symbol);
            var result = _objectMapper.Map<NFTCollectionExtensionIndex, NFTForSaleDto>(collectionExtension);
            result.CollectionName = collectionExtension.TokenName;
            result.OfDtoInfo(nftInfoIndex, lastDealInfo);

            const int maxResultCount = 20;
            var getNftListingsDto = new GetNFTListingsDto
            {
                ChainId = nftInfoIndex.ChainId,
                Symbol = nftInfoIndex.Symbol,
                SkipCount = 0,
                MaxResultCount = maxResultCount
            };

            if (!input.ExcludedAddress.IsNullOrEmpty())
            {
                getNftListingsDto.ExcludedAddress = input.ExcludedAddress;
            }

            long availableQuantity = 0;
            long remain = 0;
            do
            {
                var listingDto = await _nftListingProvider.GetNFTListingsAsync(getNftListingsDto);
                listingDto.Items.ToList().ForEach(listing => { availableQuantity += listing.RealQuantity; });

                getNftListingsDto.SkipCount += maxResultCount;
                remain = listingDto.TotalCount - getNftListingsDto.SkipCount;
            } while (remain > 0);

            result.AvailableQuantity = availableQuantity;

            var indexerNFTOffer = await _nftOfferProvider.GetMaxOfferInfoAsync(input.Id);
            if (indexerNFTOffer == null || indexerNFTOffer.Id.IsNullOrEmpty())
            {
                return result;
            }

            result.MaxOfferPrice = indexerNFTOffer.Price;
            result.MaxOfferPriceSymbol = indexerNFTOffer.PurchaseToken.Symbol;

            return result;
        }

        private static CompositeNFTInfoIndexDto MapForSeedBriefInfoDto(SeedSymbolIndex seedSymbolIndex,
            Dictionary<string, IndexerNFTOffer> maxOfferDict, Dictionary<string, AccountDto> accountDtoDict)
        {
            maxOfferDict.TryGetValue(seedSymbolIndex.Id, out var maxOffer);
            var accountDto = new AccountDto();
            if (!seedSymbolIndex.RealOwner.IsNullOrEmpty())
            {
                accountDtoDict.TryGetValue(seedSymbolIndex.RealOwner, out var temAccountDto);
                accountDto = temAccountDto;
            }

            var (temDescription, temPrice) = seedSymbolIndex.GetDescriptionAndPrice(maxOffer?.Price ?? 0);

            var temLatestDealPrice = seedSymbolIndex.LatestDealPrice <= 0 && !seedSymbolIndex.HasAuctionFlag
                ? seedSymbolIndex.AuctionPrice
                : seedSymbolIndex.LatestDealPrice;
            if (temLatestDealPrice == 0)
            {
                temLatestDealPrice = CommonConstant.DefaultValueNone;
            }

            return new CompositeNFTInfoIndexDto
            {
                CollectionSymbol = NFTSymbolBasicConstants.SeedCollectionSymbol,
                NFTSymbol = seedSymbolIndex.SeedOwnedSymbol,
                PreviewImage = seedSymbolIndex.SeedImage,
                PriceDescription = temDescription,
                Price = temPrice,
                Id = seedSymbolIndex.Id,
                TokenName = seedSymbolIndex.TokenName,
                //IssueChainId = seedSymbolIndex.IssueChainId,
                IssueChainIdStr = ChainHelper.ConvertChainIdToBase58(seedSymbolIndex.IssueChainId),
                //ChainId = ChainHelper.ConvertBase58ToChainId(seedSymbolIndex.ChainId),
                ChainIdStr = seedSymbolIndex.ChainId,
                ListingPrice = seedSymbolIndex.HasAuctionFlag
                    ? seedSymbolIndex.MaxAuctionPrice
                    : (seedSymbolIndex.HasListingFlag
                        ? seedSymbolIndex.MinListingPrice
                        : CommonConstant.DefaultValueNone),
                ListingPriceCreateTime = seedSymbolIndex.HasAuctionFlag
                    ? seedSymbolIndex.AuctionDateTime
                    : seedSymbolIndex.LatestListingTime,
                OfferPrice = maxOffer?.Price ?? CommonConstant.DefaultValueNone,
                LatestDealPrice = temLatestDealPrice,
                AllOwnerCount = CommonConstant.IntOne,
                RealOwner = accountDto
            };
        }

        private static CompositeNFTInfoIndexDto MapForSeedBriefInfoDtoV2(SeedSymbolIndex seedSymbolIndex,
            Dictionary<string, IndexerNFTOffer> maxOfferDict, Dictionary<string, AccountDto> accountDtoDict,
            Dictionary<string, IndexerNFTListingInfo> listDtoDict,
            List<UserBalanceIndex> balanceList)
        {
            maxOfferDict.TryGetValue(seedSymbolIndex.Id, out var maxOffer);
            listDtoDict.TryGetValue(seedSymbolIndex.Id, out var minList);
            var nftDecimals = 0;
            var balance = balanceList.IsNullOrEmpty()
                ? null
                : balanceList.FirstOrDefault(x => x.NFTInfoId == seedSymbolIndex.Id);

            var accountDto = new AccountDto();
            if (!seedSymbolIndex.RealOwner.IsNullOrEmpty())
            {
                accountDtoDict.TryGetValue(seedSymbolIndex.RealOwner, out var temAccountDto);
                accountDto = temAccountDto;
            }

            var (temDescription, temPrice) = seedSymbolIndex.GetDescriptionAndPrice(maxOffer?.Price ?? 0);

            var temLatestDealPrice = seedSymbolIndex.LatestDealPrice <= 0 && !seedSymbolIndex.HasAuctionFlag
                ? seedSymbolIndex.AuctionPrice
                : seedSymbolIndex.LatestDealPrice;
            if (temLatestDealPrice == 0)
            {
                temLatestDealPrice = CommonConstant.DefaultValueNone;
            }

            var showPrice = "--";
            var hasOwnerListingFlag = false;
            if (minList != null && minList.Prices > 0)
            {
                showPrice = minList.Prices.ToString();
            }
            else if (maxOffer != null && maxOffer.Price > 0)
            {
                showPrice = maxOffer.Price.ToString();
            }

            var profileInfo = new ProfileInfo()
            {
                MinListingPrice = minList == null ? null : minList.Prices,
                BestOfferPrice = maxOffer == null ? null : maxOffer.Price,
                ShowPrice = showPrice,
                Decimal = nftDecimals,
                Balance = balance == null ? 0 : balance.Amount / (decimal)Math.Pow(10, balance.Decimals)
            };


            return new CompositeNFTInfoIndexDto
            {
                CollectionSymbol = NFTSymbolBasicConstants.SeedCollectionSymbol,
                NFTSymbol = seedSymbolIndex.SeedOwnedSymbol,
                PreviewImage = seedSymbolIndex.SeedImage,
                PriceDescription = temDescription,
                Price = temPrice,
                Id = seedSymbolIndex.Id,
                TokenName = seedSymbolIndex.TokenName,
                IssueChainIdStr = ChainHelper.ConvertChainIdToBase58(seedSymbolIndex.IssueChainId),
                ChainIdStr = seedSymbolIndex.ChainId,
                ListingPrice = seedSymbolIndex.HasAuctionFlag
                    ? seedSymbolIndex.MaxAuctionPrice
                    : (seedSymbolIndex.HasListingFlag
                        ? seedSymbolIndex.MinListingPrice
                        : CommonConstant.DefaultValueNone),
                ListingPriceCreateTime = seedSymbolIndex.HasAuctionFlag
                    ? seedSymbolIndex.AuctionDateTime
                    : seedSymbolIndex.LatestListingTime,
                OfferPrice = maxOffer?.Price ?? CommonConstant.DefaultValueNone,
                LatestDealPrice = temLatestDealPrice,
                AllOwnerCount = CommonConstant.IntOne,
                RealOwner = accountDto,
                ProfileInfo = profileInfo
            };
        }

        private static CompositeNFTInfoIndexDto MapForNftBriefInfoDto(IndexerNFTInfo nftInfoIndex,
            Dictionary<string, IndexerNFTOffer> maxOfferDict, Dictionary<string, AccountDto> accountDtoDict)
        {
            var accountDto = new AccountDto();
            maxOfferDict.TryGetValue(nftInfoIndex.Id, out var maxOffer);
            if (!nftInfoIndex.RealOwner.IsNullOrEmpty())
            {
                accountDtoDict.TryGetValue(nftInfoIndex.RealOwner, out var temAccountDto);
                accountDto = temAccountDto;
            }

            var (temDescription, temPrice) = nftInfoIndex.GetDescriptionAndPrice(maxOffer?.Price ?? 0);

            return new CompositeNFTInfoIndexDto
            {
                CollectionSymbol = nftInfoIndex.CollectionSymbol,
                NFTSymbol = nftInfoIndex.Symbol,
                PreviewImage = nftInfoIndex.ImageUrl,
                PriceDescription = temDescription,
                Price = temPrice,
                Id = nftInfoIndex.Id,
                TokenName = nftInfoIndex.TokenName,
                IssueChainIdStr = ChainHelper.ConvertChainIdToBase58(nftInfoIndex.IssueChainId),
                ChainIdStr = nftInfoIndex.ChainId,
                TraitPairsDictionary = nftInfoIndex.TraitPairsDictionary
                    .Select(item => new MetadataDto { Key = item.Key, Value = item.Value }).ToList(),
                Generation = nftInfoIndex.Generation,
                ListingPrice = nftInfoIndex.ListingPrice,
                ListingPriceCreateTime = nftInfoIndex.LatestListingTime,
                OfferPrice = maxOffer?.Price ?? CommonConstant.DefaultValueNone,
                LatestDealPrice = nftInfoIndex.LatestDealPrice,
                AllOwnerCount = nftInfoIndex.AllOwnerCount,
                RealOwner = accountDto,
                Rank = nftInfoIndex.Rank,
                Rarity = nftInfoIndex.Rarity,
                Level = nftInfoIndex.Level,
                Grade = nftInfoIndex.Grade,
                Star = nftInfoIndex.Star,
                Describe = nftInfoIndex.Describe
            };
        }

        private static CompositeNFTInfoIndexDto MapForNftBriefInfoDtoV2(IndexerNFTInfo nftInfoIndex,
            Dictionary<string, IndexerNFTOffer> maxOfferDict, Dictionary<string, AccountDto> accountDtoDict,
            Dictionary<string, IndexerNFTListingInfo> listDtoDict, Dictionary<string, int> nftDecimalDict,
            List<UserBalanceIndex> balanceList)
        {
            var accountDto = new AccountDto();
            //maxOfferDict.TryGetValue(nftInfoIndex.Id, out var maxOffer);
            //listDtoDict.TryGetValue(nftInfoIndex.Id, out var minList);
            nftDecimalDict.TryGetValue(nftInfoIndex.Id, out var nftDecimals);
            var balance = balanceList.IsNullOrEmpty()
                ? null
                : balanceList.FirstOrDefault(x => x.NFTInfoId == nftInfoIndex.Id);
            if (!nftInfoIndex.RealOwner.IsNullOrEmpty())
            {
                accountDtoDict.TryGetValue(nftInfoIndex.RealOwner, out var temAccountDto);
                accountDto = temAccountDto;
            }

            var showPrice = "--";
            var hasOwnerListingFlag = false;
            // if (minList != null && minList.Prices > 0)
            // {
            //     showPrice = minList.Prices.ToString();
            // }
            // else if (maxOffer != null && maxOffer.Price > 0)
            // {
            //     showPrice = maxOffer.Price.ToString();
            // } todo v2
            var maxOfferPrice = nftInfoIndex.MaxOfferPrice != null && nftInfoIndex.MaxOfferPrice > 0
                ? nftInfoIndex.MaxOfferPrice
                : CommonConstant.DefaultValueNone;
            if (nftInfoIndex.MinListingPrice != null && nftInfoIndex.MinListingPrice > 0)
            {
                showPrice = nftInfoIndex.MinListingPrice.ToString();
            }
            else if (maxOfferPrice > 0)
            {
                showPrice = maxOfferPrice.ToString();
            }


            var profileInfo = new ProfileInfo()
            {
                MinListingPrice = nftInfoIndex.MinListingPrice,
                BestOfferPrice = nftInfoIndex.MaxOfferPrice,
                ShowPrice = showPrice,
                Decimal = nftDecimals,
                Balance = balance == null ? 0 : (decimal)balance.Amount / (decimal)Math.Pow(10, balance.Decimals)
            };

            var (temDescription, temPrice) = nftInfoIndex.GetDescriptionAndPrice(nftInfoIndex?.MaxOfferPrice ?? 0);

            return new CompositeNFTInfoIndexDto
            {
                CollectionSymbol = nftInfoIndex.CollectionSymbol,
                NFTSymbol = nftInfoIndex.Symbol,
                PreviewImage = nftInfoIndex.ImageUrl,
                PriceDescription = temDescription,
                Price = temPrice,
                Id = nftInfoIndex.Id,
                TokenName = nftInfoIndex.TokenName,
                IssueChainIdStr = ChainHelper.ConvertChainIdToBase58(nftInfoIndex.IssueChainId),
                ChainIdStr = nftInfoIndex.ChainId,
                TraitPairsDictionary = nftInfoIndex.TraitPairsDictionary
                    .Select(item => new MetadataDto { Key = item.Key, Value = item.Value }).ToList(),
                Generation = nftInfoIndex.Generation,
                ListingPrice = nftInfoIndex.ListingPrice,
                ListingPriceCreateTime = nftInfoIndex.LatestListingTime,
                OfferPrice = maxOfferPrice,
                LatestDealPrice = nftInfoIndex.LatestDealPrice,
                AllOwnerCount = nftInfoIndex.AllOwnerCount,
                RealOwner = accountDto,
                Rank = nftInfoIndex.Rank,
                Rarity = nftInfoIndex.Rarity,
                Level = nftInfoIndex.Level,
                Grade = nftInfoIndex.Grade,
                Star = nftInfoIndex.Star,
                Describe = nftInfoIndex.Describe,
                ProfileInfo = profileInfo
            };
        }

        private async Task<Dictionary<string, IndexerNFTOffer>> GetMaxOfferInfosAsync(List<string> nftIds)
        {
            var tasks = nftIds.Select(nftId => _nftOfferProvider.GetMaxOfferInfoAsync(nftId)).ToList();
            var maxOfferResults = await Task.WhenAll(tasks);
            var maxOfferDict = maxOfferResults.Where(offer => offer != null && !offer.BizInfoId.IsNullOrEmpty())
                .ToDictionary(offer => offer.BizInfoId, offer => offer);
            return maxOfferDict;
        }

        private async Task<Dictionary<string, IndexerNFTListingInfo>> GetMyMinListInfosAsync(List<string> symbols,
            string address, string chainId, string from)
        {
            var tasks = new List<Task<PagedResultDto<IndexerNFTListingInfo>>>();
            foreach (var symbol in symbols)
            {
                var input = new GetNFTListingsDto()
                {
                    ChainId = chainId,
                    Symbol = symbol,
                    Address = address,
                    SkipCount = 0,
                    MaxResultCount = 1,
                    ExcludedAddress = "",
                    BlockHeight = 0
                };
                tasks.Add(_nftListingProvider.GetNFTListingsAsync(input));
            }

            var minListResults = await Task.WhenAll(tasks);
            _logger.LogInformation("GetMyMinListInfosAsync from:{from} minListResultsCount:{minListResults}", from,
                minListResults.Length);

            var minListDict = minListResults.Where(result =>
                    result != null && result.TotalCount != 0 && result.Items.Count != 0)
                .ToDictionary(
                    result => (result.Items.FirstOrDefault().ChainId + "-" + result.Items.FirstOrDefault().Symbol),
                    result => result.Items.FirstOrDefault());
            return minListDict;
        }

        private async Task<bool> GetCanBuyFlagAsync(string chainId, string symbol, string excludedAddress)
        {
            var getNftListingsDto = new GetNFTListingsDto()
            {
                ChainId = chainId,
                Symbol = symbol,
                ExcludedAddress = excludedAddress,
                SkipCount = 0,
                MaxResultCount = 10
            };

            var listingDto = await _nftListingProvider.GetNFTListingsAsync(getNftListingsDto);

            return listingDto.TotalCount > 0;
        }

        private async Task<IndexerNFTDealInfo> GetLastDealAsync(string chainId, string symbol)
        {
            //query deal info , the default order by DealTime desc
            var dto = new GetNftDealInfoDto()
            {
                ChainId = chainId,
                Symbol = symbol,
                //desc
                SortType = 1,
                SkipCount = 0,
                MaxResultCount = 1
            };
            var dealInfos = await _nftDealInfoProvider.GetDealInfosAsync(dto);
            if (dealInfos == null || dealInfos.IndexerNftDealList.IsNullOrEmpty()) return null;
            return dealInfos.IndexerNftDealList.FirstOrDefault();
        }

        public async Task<NFTOwnerDto> GetNFTOwnersAsync(GetNFTOwnersInput input)
        {
            if (input == null || input.Id.IsNullOrWhiteSpace())
            {
                return null;
            }

            var choiceNFTInfoNewFlag = _choiceNFTInfoNewFlagOptionsMonitor?.CurrentValue?
                .ChoiceNFTInfoNewFlagIsOn ?? false;
            IndexerNFTInfo nftInfoIndex;
            if (choiceNFTInfoNewFlag)
            {
                nftInfoIndex = await _nftInfoNewSyncedProvider.GetNFTInfoIndexAsync(input.Id);
            }
            else
            {
                nftInfoIndex = await _nftInfoSyncedProvider.GetNFTInfoIndexAsync(input.Id);
            }

            if (nftInfoIndex == null)
            {
                return null;
            }

            var ret = new NFTOwnerDto
            {
                TotalCount = 0,
                Supply = nftInfoIndex.TotalSupply,
                ChainId = input.ChainId
            };

            var nftOwners = await _nftInfoProvider.GetNFTOwnersAsync(input);
            _logger.LogInformation("GetNFTOwnersAsync, id: {id}", input.Id);
            if (nftOwners == null)
            {
                return ret;
            }

            var addresses = nftOwners.IndexerNftUserBalances.Select(item => item.Address).ToList();
            var accounts = await _userAppService.GetAccountsAsync(addresses);

            var owners = new List<NFTOwnerInfo>();
            for (int i = 0; i < addresses.Count; i++)
            {
                var address = addresses[i];
                var account = accounts[address];
                var userInfo = _objectMapper.Map<AccountDto, UserInfo>(account);
                userInfo.FullAddress = FullAddressHelper.ToFullAddress(userInfo.Address, input.ChainId);
                owners.Add(
                    new NFTOwnerInfo
                    {
                        Owner = userInfo,
                        ItemsNumber = FTHelper.GetIntegerDivision(nftOwners.IndexerNftUserBalances[i].Amount,
                            nftInfoIndex.Decimals)
                    });
                _logger.LogInformation("GetNFTOwnersAsync-Add owner, address: {id}, cnt: {cnd}",
                    userInfo.Address, nftOwners.IndexerNftUserBalances[i].Amount);
            }

            ret.TotalCount = nftOwners.TotalCount;
            ret.Items = owners.Where(item => item.ItemsNumber > CommonConstant.IntZero).ToList();

            return ret;
        }

        public async Task<PagedResultDto<NFTActivityDto>> GetActivityListAsync(GetActivitiesInput input)
        {
            var activities = await _nftActivityAppService.GetListAsync(input);
            if (activities == null || activities.TotalCount == 0 || activities.Items.Count == 0) return null;

            var returnItems = new List<NFTActivityDto>();
            foreach (var activity in activities.Items)
            {
                var nftInfoIndexDto = await GetNFTInfoAsync(new GetNFTInfoInput()
                {
                    Id = activity.NFTInfoId
                });
                if (nftInfoIndexDto == null) continue;

                activity.Symbol = nftInfoIndexDto.NFTSymbol;
                activity.CollectionSymbol = nftInfoIndexDto.NFTCollection.Symbol;
                activity.CollectionName = nftInfoIndexDto.NFTCollection.TokenName;
                activity.TotalPrice = (decimal)activity.Price * activity.Amount;
                activity.PreviewImage = nftInfoIndexDto.PreviewImage;
                if (!activity.Symbol.Contains(input.FilterSymbol)) continue;
                returnItems.Add(activity);
            }

            return new PagedResultDto<NFTActivityDto>
            {
                Items = returnItems,
                TotalCount = returnItems.Count
            };
        }

        private string GetDefaultSideChainId()
        {
            var chainIds = _chainOptionsMonitor.CurrentValue.ChainInfos.Keys;
            foreach (var chainId in chainIds)
            {
                if (!chainId.Equals(_defaultMainChain))
                {
                    return chainId;
                }
            }

            return _defaultMainChain;
        }

        public async Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetMyHoldNFTInfosAsync(GetMyHoldNFTInfosInput input)
        {
            var queryUserBalanceIndexInput = new QueryUserBalanceIndexInput()
            {
                Address = input.Address,
                QueryType = input.QueryType,
                SkipCount = CommonConstant.IntZero,
                CollectionIdList = input.CollectionIds
            };
            var userBalanceList =
                await _userBalanceIndexProvider.GetValidUserBalanceInfosAsync(queryUserBalanceIndexInput);
            if (userBalanceList.IsNullOrEmpty())
            {
                return new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = CommonConstant.IntZero,
                    Items = new List<CompositeNFTInfoIndexDto>()
                };
            }

            var nftIds = userBalanceList.Select(i => i.NFTInfoId).Distinct().ToList();
            var nftSymbols = userBalanceList.Select(i => i.Symbol).Distinct().ToList();
            var fuzzySearchSwitch = _fuzzySearchOptionsMonitor.CurrentValue.FuzzySearchSwitch;

            var getCompositeNFTInfosInput = new GetCompositeNFTInfosInput()
            {
                NFTIdList = nftIds,
                HasAuctionFlag = input.HasAuctionFlag,
                HasListingFlag = input.HasListingFlag,
                HasOfferFlag = input.HasOfferFlag,
                SkipCount = input.SkipCount,
                MaxResultCount = input.MaxResultCount,
                Sorting = input.Sorting,
                SearchParam = input.KeyWord,
                PriceLow = input.PriceLow,
                PriceHigh = input.PriceHigh,
                FuzzySearchSwitch = fuzzySearchSwitch,
                PageFrom = PageFromEnum.OTHER
            };
            var result = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();
            var seedPageResult = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();
            var nftPageResult = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();

            {
                var seedResult = await _seedSymbolSyncedProvider.GetSeedBriefInfosAsync(getCompositeNFTInfosInput);
                //to get max offers
                var maxOfferDict = await GetMaxOfferInfosAsync(seedResult.Item2.Select(info => info.Id).ToList());

                var accountDtoDict =
                    await _userAppService.GetAccountsAsync(seedResult.Item2.Select(info => info.RealOwner).ToList());

                seedPageResult = new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = seedResult.Item1,
                    Items = seedResult.Item2.Select(item => MapForSeedBriefInfoDto(item, maxOfferDict, accountDtoDict))
                        .ToList()
                };
            }

            {
                var nftResult = await _nftInfoNewSyncedProvider.GetNFTBriefInfosAsync(getCompositeNFTInfosInput);

                var maxOfferDict = await GetMaxOfferInfosAsync(nftResult.Item2.Select(info => info.Id).ToList());

                var accountDtoDict =
                    await _userAppService.GetAccountsAsync(nftResult.Item2.Select(info => info.RealOwner).ToList());

                nftPageResult = new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = nftResult.Item1,
                    Items = nftResult.Item2.Select(item => MapForNftBriefInfoDto(item, maxOfferDict, accountDtoDict))
                        .ToList()
                };
            }
            result = new PagedResultDto<CompositeNFTInfoIndexDto>()
            {
                TotalCount = seedPageResult.TotalCount + nftPageResult.TotalCount,
                Items = seedPageResult.Items.Concat(nftPageResult.Items).ToList()
            };
            return await MapForCompositeNftInfoIndexDtoPage(result);
        }

        public async Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetMyHoldNFTInfosV2Async(
            GetMyHoldNFTInfosInput input)
        {
            var queryUserBalanceIndexInput = new QueryUserBalanceIndexInput()
            {
                Address = input.Address,
                QueryType = input.QueryType,
                SkipCount = CommonConstant.IntZero,
                CollectionIdList = input.CollectionIds
            };
            var userBalanceList =
                await _userBalanceIndexProvider.GetValidUserBalanceInfosAsync(queryUserBalanceIndexInput);
            if (userBalanceList.IsNullOrEmpty())
            {
                return new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = CommonConstant.IntZero,
                    Items = new List<CompositeNFTInfoIndexDto>()
                };
            }

            var nftIds = userBalanceList.Select(i => i.NFTInfoId).Distinct().ToList();
            // var nftSymbols = userBalanceList.Select(i => i.Symbol).Distinct().ToList();
            var fuzzySearchSwitch = _fuzzySearchOptionsMonitor.CurrentValue.FuzzySearchSwitch;

            var getCompositeNFTInfosInput = new GetCompositeNFTInfosInput()
            {
                NFTIdList = nftIds,
                HasAuctionFlag = input.HasAuctionFlag,
                HasListingFlag = input.HasListingFlag,
                HasOfferFlag = input.HasOfferFlag,
                SkipCount = 0,
                MaxResultCount = CommonConstant.ProfileTotalNumber,
                Sorting = input.Sorting,
                SearchParam = input.KeyWord,
                PriceLow = input.PriceLow,
                // PriceHigh = input.PriceHigh,
                FuzzySearchSwitch = fuzzySearchSwitch,
                PageFrom = PageFromEnum.OTHER
            };

            var result = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();
            var seedPageResult = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();
            var nftPageResult = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();

            var seedTask = Task.Run(async () =>
            {
                var seedResult = await _seedSymbolSyncedProvider.GetSeedBriefInfosAsync(getCompositeNFTInfosInput);
                var maxOfferDict = new Dictionary<string, IndexerNFTOffer>();
                // var maxOfferDict = await GetMaxOfferInfosAsync(seedResult.Item2.Select(info => info.Id).ToList());

                var minListDict = new Dictionary<string, IndexerNFTListingInfo>();
                // var minListDict = await GetMyMinListInfosAsync(seedResult.Item2.Select(info => info.Symbol).ToList(),
                //     input.Address, input.ChainList.FirstOrDefault(), "Hold-Seed");

                var accountDtoDict =
                    await _userAppService.GetAccountsAsync(seedResult.Item2.Select(info => info.RealOwner).ToList());

                var request = new GetMyCreateNFTInfosInput()
                {
                    SkipCount = input.SkipCount,
                    MaxResultCount = input.MaxResultCount,
                    Sorting = input.Sorting,
                    PriceHigh = input.PriceHigh,
                    PriceLow = input.PriceLow,
                    HasListingFlag = input.HasListingFlag,
                    Address = input.Address
                };

                return PageSeedBriefInfoIndexDto(request, seedResult.Item2, maxOfferDict, accountDtoDict, minListDict,
                    userBalanceList);
            });

            var nftTask = Task.Run(async () =>
            {
                var nftResult = await GetAllNFTBriefInfosAsync(getCompositeNFTInfosInput);
                var nftDecimalDict = nftResult.Item2.Where(info => info != null && !info.Id.IsNullOrEmpty())
                    .GroupBy(info => info.Id)
                    .ToDictionary(info => info.Key, info => info.First().Decimals);

                var maxOfferDict = new Dictionary<string, IndexerNFTOffer>();
                //todo v2
                // var maxOfferDict = await GetMaxOfferInfosAsync(nftResult.Item2.Select(info => info.Id).ToList());

                var minListDict = new Dictionary<string, IndexerNFTListingInfo>();
                //todo v2
                // var minListDict = await GetMyMinListInfosAsync(nftResult.Item2.Select(info => info.Symbol).ToList(),
                //    input.Address, input.ChainList.FirstOrDefault(), "Hold-NFT");
                var accountDtoDict =
                    await _userAppService.GetAccountsAsync(nftResult.Item2.Select(info => info.RealOwner).ToList());

                var request = new GetMyCreateNFTInfosInput()
                {
                    SkipCount = input.SkipCount,
                    MaxResultCount = input.MaxResultCount,
                    Sorting = input.Sorting,
                    PriceHigh = input.PriceHigh,
                    PriceLow = input.PriceLow,
                    HasListingFlag = input.HasListingFlag,
                    Address = input.Address
                };

                return PageCompositeNFTInfoIndexDto(request, nftResult.Item2, maxOfferDict, accountDtoDict, minListDict,
                    nftDecimalDict, userBalanceList);
            });

            await Task.WhenAll(seedTask, nftTask);

            seedPageResult = await seedTask;
            nftPageResult = await nftTask;

            result = new PagedResultDto<CompositeNFTInfoIndexDto>()
            {
                TotalCount = seedPageResult.TotalCount + nftPageResult.TotalCount,
                Items = seedPageResult.Items.Concat(nftPageResult.Items).ToList()
            };
            return await MapForCompositeNftInfoIndexDtoPage(result);
        }

        public async Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetMyCreatedNFTInfosAsync(
            GetMyCreateNFTInfosInput input)
        {
            var fuzzySearchSwitch = _fuzzySearchOptionsMonitor.CurrentValue.FuzzySearchSwitch;

            //query nft infos
            var getCompositeNFTInfosInput = new GetCompositeNFTInfosInput()
            {
                HasAuctionFlag = input.HasAuctionFlag,
                HasListingFlag = input.HasListingFlag,
                HasOfferFlag = input.HasOfferFlag,
                SkipCount = input.SkipCount,
                MaxResultCount = input.MaxResultCount,
                Sorting = input.Sorting,
                SearchParam = input.KeyWord,
                IssueAddress = input.Address,
                PriceLow = input.PriceLow,
                PriceHigh = input.PriceHigh,
                CollectionIds = input.CollectionIds,
                FuzzySearchSwitch = fuzzySearchSwitch,
                PageFrom = PageFromEnum.OTHER
            };

            var result = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();
            var seedPageResult = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();
            var nftPageResult = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();

            {
                var seedResult = await _seedSymbolSyncedProvider.GetSeedBriefInfosAsync(getCompositeNFTInfosInput);
                //to get max offers
                var maxOfferDict = await GetMaxOfferInfosAsync(seedResult.Item2.Select(info => info.Id).ToList());

                var accountDtoDict =
                    await _userAppService.GetAccountsAsync(seedResult.Item2.Select(info => info.RealOwner).ToList());

                seedPageResult = new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = seedResult.Item1,
                    Items = seedResult.Item2.Select(item => MapForSeedBriefInfoDto(item, maxOfferDict, accountDtoDict))
                        .ToList()
                };
            }

            {
                var nftResult = await _nftInfoNewSyncedProvider.GetNFTBriefInfosAsync(getCompositeNFTInfosInput);

                var maxOfferDict = await GetMaxOfferInfosAsync(nftResult.Item2.Select(info => info.Id).ToList());

                var accountDtoDict =
                    await _userAppService.GetAccountsAsync(nftResult.Item2.Select(info => info.RealOwner).ToList());

                nftPageResult = new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = nftResult.Item1,
                    Items = nftResult.Item2.Select(item => MapForNftBriefInfoDto(item, maxOfferDict, accountDtoDict))
                        .ToList()
                };
            }
            result = new PagedResultDto<CompositeNFTInfoIndexDto>()
            {
                TotalCount = seedPageResult.TotalCount + nftPageResult.TotalCount,
                Items = seedPageResult.Items.Concat(nftPageResult.Items).ToList()
            };
            return await MapForCompositeNftInfoIndexDtoPage(result);
        }

        public async Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetMyCreatedNFTInfosAsyncV2(
            GetMyCreateNFTInfosInput input)
        {
            var fuzzySearchSwitch = _fuzzySearchOptionsMonitor.CurrentValue.FuzzySearchSwitch;

            //query nft infos
            var getCompositeNFTInfosInput = new GetCompositeNFTInfosInput()
            {
                HasAuctionFlag = input.HasAuctionFlag,
                HasListingFlag = input.HasListingFlag,
                HasOfferFlag = input.HasOfferFlag,
                SkipCount = 0,
                MaxResultCount = CommonConstant.ProfileTotalNumber,
                Sorting = input.Sorting,
                SearchParam = input.KeyWord,
                IssueAddress = input.Address,
                PriceLow = input.PriceLow,
                //PriceHigh = input.PriceHigh,
                CollectionIds = input.CollectionIds,
                FuzzySearchSwitch = fuzzySearchSwitch,
                PageFrom = PageFromEnum.OTHER,
            };

            var result = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();
            var seedPageResult = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();
            var nftPageResult = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();

            var seedTask = Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                var spend1 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-Seed 1 {A}", spend1);

                var seedResult = await _seedSymbolSyncedProvider.GetSeedBriefInfosAsync(getCompositeNFTInfosInput);

                var spend2 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-Seed 2 {A} {B}", spend2, spend2 - spend1);

                var maxOfferDict = new Dictionary<string, IndexerNFTOffer>();
                //todo v2
                // var maxOfferDict = await GetMaxOfferInfosAsync(seedResult.Item2.Select(info => info.Id).ToList());

                var spend3 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-Seed 3 {A} {B}", spend3, spend3 - spend2);

                var minListDict = new Dictionary<string, IndexerNFTListingInfo>();
                //todo v2
                // var minListDict = await GetMyMinListInfosAsync(seedResult.Item2.Select(info => info.Symbol).ToList(),
                //     input.Address, input.ChainList.FirstOrDefault(), "Create-Seed");

                var spend4 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-Seed 4 {A} {B}", spend4, spend4 - spend3);

                var queryUserBalanceIndexInput = new QueryUserBalanceIndexInput
                {
                    Address = input.Address,
                    QueryType = input.QueryType,
                    SkipCount = CommonConstant.IntZero,
                    CollectionIdList = input.CollectionIds,
                    MaxResultCount = CommonConstant.ProfileTotalNumber
                };
                var userBalanceList =
                    await _userBalanceIndexProvider.GetValidUserBalanceInfosAsync(queryUserBalanceIndexInput);

                var spend5 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-Seed 5 {A} {B}", spend5, spend5 - spend4);

                var accountDtoDict =
                    await _userAppService.GetAccountsAsync(seedResult.Item2.Select(info => info.RealOwner).ToList());

                var spend6 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-Seed 6 {A} {B}", spend6, spend6 - spend5);

                var result = PageSeedBriefInfoIndexDto(input, seedResult.Item2, maxOfferDict, accountDtoDict,
                    minListDict,
                    userBalanceList);

                var spend7 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-Seed 7 {A} {B}", spend7, spend7 - spend6);

                return result;
            });

            var nftTask = Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                var spend1 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-NFT 1 {A}", spend1);

                var nftResult = await GetAllNFTBriefInfosAsync(getCompositeNFTInfosInput);

                var spend2 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-NFT 2 {A} {B}", spend2, spend2 - spend1);

                var nftDecimalDict = nftResult.Item2.Where(info => info != null && !info.Id.IsNullOrEmpty())
                    .ToDictionary(info => info.Id, info => info.Decimals);

                var spend3 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-NFT 3 {A} {B}", spend3, spend3 - spend2);

                var maxOfferDict = new Dictionary<string, IndexerNFTOffer>();
                //todo v2
                // var maxOfferDict = await GetMaxOfferInfosAsync(nftResult.Item2.Select(info => info.Id).ToList());

                var spend4 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-NFT 4 {A} {B}", spend4, spend4 - spend3);

                var minListDict = new Dictionary<string, IndexerNFTListingInfo>();
                //todo v2
                // var minListDict = await GetMyMinListInfosAsync(nftResult.Item2.Select(info => info.Symbol).ToList(),
                //     input.Address, input.ChainList.FirstOrDefault(), "Create-NFT");

                var spend5 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-NFT 5 {A} {B}", spend5, spend5 - spend4);

                var accountDtoDict =
                    await _userAppService.GetAccountsAsync(nftResult.Item2.Select(info => info.RealOwner).ToList());

                var spend6 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-NFT 6 {A} {B}", spend6, spend6 - spend5);

                var queryUserBalanceIndexInput = new QueryUserBalanceIndexInput
                {
                    Address = input.Address,
                    QueryType = input.QueryType,
                    SkipCount = CommonConstant.IntZero,
                    CollectionIdList = input.CollectionIds,
                    MaxResultCount = CommonConstant.ProfileTotalNumber,
                    KeyWord = ""
                };
                var userBalanceList =
                    await _userBalanceIndexProvider.GetValidUserBalanceInfosAsync(queryUserBalanceIndexInput);

                var spend7 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-NFT 7 {A} {B}", spend7, spend7 - spend6);

                _logger.LogInformation("GetMyCreatedNFTInfosAsyncV2 NFT userBalanceListCount:{userBalanceList}",
                    userBalanceList.Count);

                var result = PageCompositeNFTInfoIndexDto(input, nftResult.Item2, maxOfferDict, accountDtoDict,
                    minListDict,
                    nftDecimalDict, userBalanceList);

                var spend8 = stopwatch.ElapsedMilliseconds;
                Logger.LogDebug("GetMyCreatedNFTInfosAsyncV2-NFT 8 {A} {B}", spend8, spend8 - spend7);

                return result;
            });
            await Task.WhenAll(seedTask, nftTask);
            seedPageResult = await seedTask;
            nftPageResult = await nftTask;

            result = new PagedResultDto<CompositeNFTInfoIndexDto>()
            {
                TotalCount = seedPageResult.TotalCount + nftPageResult.TotalCount,
                Items = seedPageResult.Items.Concat(nftPageResult.Items).ToList()
            };
            return await MapForCompositeNftInfoIndexDtoPage(result);
        }

        private PagedResultDto<CompositeNFTInfoIndexDto> PageSeedBriefInfoIndexDto(GetMyCreateNFTInfosInput input,
            List<SeedSymbolIndex> seedResult, Dictionary<string, IndexerNFTOffer> maxOfferDict,
            Dictionary<string, AccountDto> accountDtoDict, Dictionary<string, IndexerNFTListingInfo> listDtoDict,
            List<UserBalanceIndex> balanceList)
        {
            var compositeNFTInfoIndexDtoList = seedResult.Select(item =>
                    MapForSeedBriefInfoDtoV2(item, maxOfferDict, accountDtoDict, listDtoDict, balanceList))
                .ToList();
            if (input.HasListingFlag)
            {
                compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                    .Where(x => x.ProfileInfo.MinListingPrice != null).ToList();
            }

            if (input.PriceHigh != null)
            {
                compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                    .Where(x => x.ProfileInfo.MinListingPrice <= input.PriceHigh).ToList();
            }

            if (input.PriceLow != null)
            {
                compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                    .Where(x => x.ProfileInfo.MinListingPrice >= input.PriceLow).ToList();
            }

            if (compositeNFTInfoIndexDtoList.IsNullOrEmpty())
            {
                return new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = 0,
                    Items = null
                };
            }

            if (!string.IsNullOrWhiteSpace(input.Sorting))
            {
                var sortingArray = input.Sorting.Split(" ");
                var sortedNfts =
                    compositeNFTInfoIndexDtoList.OrderByDescending(nft =>
                        nft.ProfileInfo?.MinListingPrice ?? decimal.MaxValue);
                switch (sortingArray[0])
                {
                    case "Low":
                        compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                            .OrderBy(i => i.ProfileInfo?.MinListingPrice ?? decimal.MaxValue).ToList();
                        break;
                    case "High":
                        compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                            .OrderByDescending(i => i.ProfileInfo?.MinListingPrice ?? decimal.MinValue).ToList();
                        break;
                    case "Recently":
                        compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                            .OrderBy(i => i.ProfileInfo?.MinListingPrice ?? decimal.MaxValue).ToList();
                        break;
                    default:
                        break;
                }
            }

            return new PagedResultDto<CompositeNFTInfoIndexDto>()
            {
                TotalCount = compositeNFTInfoIndexDtoList.Count,
                Items = compositeNFTInfoIndexDtoList.Skip(input.SkipCount).Take(input.MaxResultCount).ToList()
            };
        }
        [ExceptionHandler(typeof(Exception),
            Message = "NFTInfoAppService.PageCompositeNFTInfoIndexDto Exception",
            TargetType = typeof(ExceptionHandlingService),
            MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
            LogTargets = new[] { "input","nftResult","maxOfferDict","accountDtoDict","listDtoDict","nftDecimalDict","balanceList" }
        )]
        public virtual PagedResultDto<CompositeNFTInfoIndexDto> PageCompositeNFTInfoIndexDto(GetMyCreateNFTInfosInput input,
            List<IndexerNFTInfo> nftResult, Dictionary<string, IndexerNFTOffer> maxOfferDict,
            Dictionary<string, AccountDto> accountDtoDict, Dictionary<string, IndexerNFTListingInfo> listDtoDict,
            Dictionary<string, int> nftDecimalDict, List<UserBalanceIndex> balanceList)
        {
            List<CompositeNFTInfoIndexDto> compositeNFTInfoIndexDtoList = null;
            compositeNFTInfoIndexDtoList = nftResult.Select(item =>
                    MapForNftBriefInfoDtoV2(item, maxOfferDict, accountDtoDict, listDtoDict, nftDecimalDict,
                        balanceList))
                .ToList();
            if (input.HasListingFlag)
            {
                compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                    .Where(x => x.ProfileInfo.MinListingPrice != null).ToList();
            }

            if (input.PriceHigh != null)
            {
                compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                    .Where(x => x.ProfileInfo.MinListingPrice <= input.PriceHigh).ToList();
            }

            if (input.PriceLow != null)
            {
                compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                    .Where(x => x.ProfileInfo.MinListingPrice >= input.PriceLow).ToList();
            }

            if (compositeNFTInfoIndexDtoList.IsNullOrEmpty())
            {
                return new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = 0,
                    Items = null
                };
            }

            if (!string.IsNullOrWhiteSpace(input.Sorting))
            {
                var sortingArray = input.Sorting.Split(" ");
                // var sortedNfts =
                //     compositeNFTInfoIndexDtoList.OrderByDescending(nft =>
                //         nft.ProfileInfo?.MinListingPrice ?? decimal.MaxValue);
                switch (sortingArray[0])
                {
                    case "Low":
                        compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                            .OrderBy(i => i.ProfileInfo?.MinListingPrice ?? decimal.MaxValue).ToList();
                        break;
                    case "High":
                        compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                            .OrderByDescending(i => i.ProfileInfo?.MinListingPrice ?? decimal.MinValue).ToList();
                        break;
                    case "Recently":
                        compositeNFTInfoIndexDtoList = compositeNFTInfoIndexDtoList
                            .OrderBy(i => i.ProfileInfo?.MinListingPrice ?? decimal.MaxValue).ToList();
                        break;
                    default:
                        break;
                }
            }

            return new PagedResultDto<CompositeNFTInfoIndexDto>()
            {
                TotalCount = compositeNFTInfoIndexDtoList.Count,
                Items = compositeNFTInfoIndexDtoList.Skip(input.SkipCount).Take(input.MaxResultCount).ToList()
            };
        }

        private async Task<Tuple<long, List<IndexerNFTInfo>>> GetAllNFTBriefInfosAsync(GetCompositeNFTInfosInput input)
        {
            var nftResult = await _nftInfoNewSyncedProvider.GetNFTBriefInfosAsync(input);
            if (nftResult == null || nftResult.Item1 == 0)
            {
                return nftResult;
            }

            var allNFTList = nftResult.Item2;
            var allCount = nftResult.Item1;
            // while (allCount > allNFTList.Count)
            // {
            //     var nextPageNFTResult = await _nftInfoNewSyncedProvider.GetNFTBriefInfosAsync(input);
            //     if (nextPageNFTResult == null || nextPageNFTResult.Item1 == 0)
            //     {
            //         break;
            //     }
            //
            //     allCount += nextPageNFTResult.Item1;
            //     allNFTList.AddRange(nextPageNFTResult.Item2);
            // }

            return new Tuple<long, List<IndexerNFTInfo>>(allCount, allNFTList);
        }
    }
}