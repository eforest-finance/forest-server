using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Entities;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.Helper;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
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
        private readonly IBus _bus;

        private readonly IOptionsMonitor<ResetNFTSyncHeightExpireMinutesOptions>
            _resetNFTSyncHeightExpireMinutesOptionsMonitor;

        private readonly IOptionsMonitor<ChoiceNFTInfoNewFlagOptions>
            _choiceNFTInfoNewFlagOptionsMonitor;

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
            IBus bus,
            INFTOfferProvider nftOfferProvider,
            INFTListingProvider nftListingProvider,
            INFTDealInfoProvider nftDealInfoProvider,
            IInscriptionProvider inscriptionProvider,
            INFTCollectionExtensionProvider nftCollectionExtensionProvider,
            NFTCollectionAppService nftCollectionAppService,
            IDistributedCache<string> distributedCacheForHeight,
            IGraphQLProvider graphQlProvider,
            IOptionsMonitor<ResetNFTSyncHeightExpireMinutesOptions> resetNFTSyncHeightExpireMinutesOptionsMonitor,
            IOptionsMonitor<ChoiceNFTInfoNewFlagOptions> choiceNFTInfoNewFlagOptionsMonitor)
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
            _graphQlProvider = graphQlProvider;
            _bus = bus;
        }

        public async Task<long> QueryItemCountForNFTCollectionWithTraitKeyAsync(string key,
            string nftCollectionId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(nftCollectionId)));
            mustQuery.Add(q =>
                q.Term(i => i.Field(f => f.CountedFlag).Value(true)));

            var nestedQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            nestedQuery.Add(q => q
                .Nested(n => n
                    .Path(CommonConstant.ES_NFT_TraitPairsDictionary_Path)
                    .Query(nq => nq
                        .Bool(nb => nb
                            .Must(nm => nm
                                .Match(m => m
                                    .Field(f => f.TraitPairsDictionary.First().Key)
                                    .Query(key)
                                )
                            )
                        )
                    )
                )
            );
            mustQuery.AddRange(nestedQuery);

            QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var result = await _nftInfoNewIndexRepository.GetSortListAsync(Filter, skip: CommonConstant.IntZero,
                limit: CommonConstant.IntZero);
            if (result?.Item1 != null && result?.Item1 != CommonConstant.EsLimitTotalNumber)
            {
                return result.Item1;
            }

            return await QueryRealCountAsync(mustQuery);
        }

        public async Task<long> QueryItemCountForNFTCollectionWithTraitPairAsync(string key, string value,
            string nftCollectionId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(nftCollectionId)));
            mustQuery.Add(q =>
                q.Term(i => i.Field(f => f.CountedFlag).Value(true)));

            var nestedQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            nestedQuery.Add(q => q
                .Nested(n => n
                    .Path(CommonConstant.ES_NFT_TraitPairsDictionary_Path)
                    .Query(nq => nq
                        .Bool(nb => nb
                            .Must(nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Key)
                                        .Query(key)
                                    ),
                                nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Value)
                                        .Query(value)
                                    )
                            )
                        )
                    )
                )
            );
            mustQuery.AddRange(nestedQuery);

            QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var result = await _nftInfoNewIndexRepository.GetSortListAsync(Filter, skip: CommonConstant.IntZero,
                limit: CommonConstant.IntZero);
            if (result?.Item1 != null && result?.Item1 != CommonConstant.EsLimitTotalNumber)
            {
                return result.Item1;
            }

            return await QueryRealCountAsync(mustQuery);
        }


        public async Task<long> QueryItemCountForNFTCollectionGenerationAsync(string nftCollectionId,
            int generation)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(nftCollectionId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Generation).Value(generation)));
            mustQuery.Add(q =>
                q.Term(i => i.Field(f => f.CountedFlag).Value(true)));
            
            QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var result = await _nftInfoNewIndexRepository.GetSortListAsync(Filter, skip: CommonConstant.IntZero,
                limit: CommonConstant.IntZero);
            if (result?.Item1 != null && result?.Item1 != CommonConstant.EsLimitTotalNumber)
            {
                return result.Item1;
            }

            return await QueryRealCountAsync(mustQuery);
        }

        public async Task<NFTInfoNewIndex> QueryFloorPriceNFTForNFTWithTraitPair(string key, string value,
            string nftCollectionId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(nftCollectionId)));
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.Supply).GreaterThan(CommonConstant.IntZero)));
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.ListingPrice).GreaterThan(CommonConstant.IntZero)));

            var nowStr = DateTime.UtcNow.ToString("o");
            mustQuery.Add(q => q.DateRange(i => i.Field(f => f.ListingEndTime).GreaterThan(nowStr)));

            var nestedQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            nestedQuery.Add(q => q
                .Nested(n => n
                    .Path(CommonConstant.ES_NFT_TraitPairsDictionary_Path)
                    .Query(nq => nq
                        .Bool(nb => nb
                            .Must(nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Key)
                                        .Query(key)
                                    ),
                                nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Value)
                                        .Query(value)
                                    )
                            ))
                    )
                )
            );

            mustQuery.AddRange(nestedQuery);

            QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var result = await _nftInfoNewIndexRepository.GetListAsync(Filter
                ,skip: CommonConstant.IntZero,
                limit: CommonConstant.IntOne,
                sortType: SortOrder.Ascending, sortExp: o => o.ListingPrice);
            return result?.Item2?.FirstOrDefault();
        }

        public async Task<PagedResultDto<UserProfileNFTInfoIndexDto>> GetNFTInfosForUserProfileAsync(
            GetNFTInfosProfileInput input)
        {
            //query nft infos
            var choiceNFTInfoNewFlag = _choiceNFTInfoNewFlagOptionsMonitor?.CurrentValue?
                .ChoiceNFTInfoNewFlagIsOn ?? false;
            IndexerNFTInfos nftInfos;
            if (choiceNFTInfoNewFlag)
            {
                nftInfos = await _nftInfoNewSyncedProvider.GetNFTInfosUserProfileAsync(input);
            }
            else
            {
                nftInfos = await _nftInfoSyncedProvider.GetNFTInfosUserProfileAsync(input);
            }
            
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

            var result = await BuildNFTInfoIndexListAsync(input.Address, resultNftInfos);
            return new PagedResultDto<UserProfileNFTInfoIndexDto>
            {
                Items = _objectMapper.Map<List<NFTInfoIndexDto>, List<UserProfileNFTInfoIndexDto>>(result),
                TotalCount = (long)(totalRecordCount == null ? 0 : totalRecordCount)
            };
        }

        public async Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetCompositeNFTInfosAsync(
            GetCompositeNFTInfosInput input)
        {
            var result = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();
            
            var choiceNFTInfoNewFlag = _choiceNFTInfoNewFlagOptionsMonitor?.CurrentValue?
                .ChoiceNFTInfoNewFlagIsOn ?? false;
            
            if (input.CollectionType.Equals(CommonConstant.CollectionTypeSeed))
            {
                var seedResult = await _seedSymbolSyncedProvider.GetSeedBriefInfosAsync(input);
                //to get max offers
                var maxOfferDict = await GetMaxOfferInfosAsync(seedResult.Item2.Select(info => info.Id).ToList());

                result = new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = seedResult.Item1,
                    Items = seedResult.Item2.Select(item => MapForSeedBriefInfoDto(item, maxOfferDict)).ToList()
                };
            }

            if (input.CollectionType.Equals(CommonConstant.CollectionTypeNFT))
            {
                Tuple<long, List<NFTInfoIndex>> nftResult = null;
                
                if (choiceNFTInfoNewFlag)
                {
                    nftResult = await _nftInfoNewSyncedProvider.GetNFTBriefInfosAsync(input);
                }
                else
                {
                    nftResult = await _nftInfoSyncedProvider.GetNFTBriefInfosAsync(input);
                }
                
                var maxOfferDict = await GetMaxOfferInfosAsync(nftResult.Item2.Select(info => info.Id).ToList());

                result = new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = nftResult.Item1,
                    Items = nftResult.Item2.Select(item => MapForNftBriefInfoDto(item, maxOfferDict)).ToList()
                };
            }

            try
            {
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

                var resetNFTSyncHeightFlagCacheKey = choiceNFTInfoNewFlag
                    ? CommonConstant.ResetNFTNewSyncHeightFlagCacheKey
                    : CommonConstant.ResetNFTSyncHeightFlagCacheKey;

                var resetSyncHeightFlag =
                    await _distributedCacheForHeight.GetAsync(resetNFTSyncHeightFlagCacheKey);
                _logger.Debug(
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

                    await _distributedEventBus.PublishAsync(new NFTResetFlagEto
                    {
                        FlagDesc = resetNFTSyncHeightFlagCacheKey,
                        Minutes = resetNftSyncHeightExpireMinutes
                    });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Something is wrong {Message}", e.Message);
            }

            return await MapForCompositeNftInfoIndexDtoPage(result);
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

        public async Task<NFTInfoIndexDto> GetNFTInfoAsync(GetNFTInfoInput input)
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

            //convert listing price
            nftInfoIndex = MapForIndexerNFTInfo(nftInfoIndex);
            var addresses = GetAddresses(nftInfoIndex);
            var accounts = await _userAppService.GetAccountsAsync(addresses);
            var nftExtensions =
                await _nftInfoExtensionProvider.GetNFTInfoExtensionsAsync(new List<string> { nftInfoIndex.Id });
            var collectionInfos = await _nftCollectionProvider.GetNFTCollectionIndexByIdsAsync(
                new List<string> { nftInfoIndex.CollectionId });
            var nftInfoIndexDto =
                MapForIndexerNFTInfos(nftInfoIndex, accounts, nftExtensions, collectionInfos);
            //set default true
            var canBuyFlag = true;
            if (!input.Address.IsNullOrWhiteSpace())
            {
                canBuyFlag = await GetCanBuyFlagAsync(nftInfoIndex.ChainId, nftInfoIndex.Symbol, input.Address);
            }

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
            nftInfoIndexDto = await BuildShowPriceTypeAsync(input.Address, nftInfoIndex.ChainId, nftInfoIndex.Symbol,
                nftInfoIndexDto);

            if (nftInfoIndexDto?.Metadata?.Where(item => item.Key.Equals(CommonConstant.MetadataInscriptionImageKey))
                    .ToList()
                    .FirstOrDefault() == null)
            {
                return nftInfoIndexDto;
            }
            var tick = SymbolHelper.GainInscriptionInfoTick(nftInfoIndex.Symbol);
            try
            {
                var inscriptionInfoDto =
                    await _inscriptionProvider.GetIndexerInscriptionInfoAsync(nftInfoIndex.ChainId, tick);
                if (inscriptionInfoDto != null && inscriptionInfoDto.MintLimit == 0)
                {
                    inscriptionInfoDto.MintLimit = CommonConstant.DefaultValueNone;
                }

                nftInfoIndexDto.InscriptionInfo = inscriptionInfoDto;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Query inscriptionInfo from graphQl error tick={Tick}", tick);
            }

            return nftInfoIndexDto;
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
        
        private async Task<NFTInfoIndexDto> BuildShowPriceTypeAsync(string address, string chainId, string symbol,
            NFTInfoIndexDto nftInfoIndexDto)
        {
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
                var getMyNftListingsDto = new GetNFTListingsDto()
                {
                    ChainId = chainId,
                    Symbol = symbol,
                    SkipCount = 0,
                    MaxResultCount = 1
                };
                var listingDto = await _nftListingProvider.GetNFTListingsAsync(getMyNftListingsDto);
                if (listingDto != null && listingDto.TotalCount > 0)
                {
                    nftInfoIndexDto.ShowPriceType = ShowPriceType.MYMINLISTING.ToString();
                    return MapMinListingInfo(nftInfoIndexDto, listingDto.Items[0]);
                }
            }

            //maxOffer
            {
                var indexerNFTOffer = await _nftOfferProvider.GetMaxOfferInfoAsync(nftInfoIndexDto.Id);
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
            List<IndexerNFTInfo> nftInfos)
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
                .Select(o => MapForIndexerNFTInfos(o, accounts, nftExtensions, collectionInfos)).ToList();

            return result;
        }

        private NFTInfoIndexDto MapForIndexerNFTInfos(IndexerNFTInfo index,
            Dictionary<string, AccountDto> accounts,
            Dictionary<string, NFTInfoExtensionIndex> nftInfoExtensions,
            Dictionary<string, IndexerNFTCollection> nftCollections)
        {
            var info = _objectMapper.Map<IndexerNFTInfo, NFTInfoIndexDto>(index);

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
            info.TotalQuantity = index.TotalSupply;
            if (index.ExternalInfoDictionary != null)
            {
                info.Metadata = index.ExternalInfoDictionary
                    .Select(kv => new MetadataDto { Key = kv.Key, Value = kv.Value }).ToList();
            }

            var nftImageUrl = info?.Metadata?.FirstOrDefault(o => o.Key == "__nft_image_url");

            if (info.PreviewImage.IsNullOrEmpty())
            {
                info.PreviewImage = nftImageUrl?.Value;
            }

            if (info.PreviewImage.IsNullOrEmpty())
            {
                info.PreviewImage = index.ImageUrl;
            }

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
        public async Task CreateNFTInfoExtensionAsync(CreateNFTExtensionInput input)
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
            if (string.IsNullOrEmpty(nftInfoId) || string.IsNullOrEmpty(chainId))
            {
                return;
            }

            if (!SymbolHelper.CheckSymbolIsCommonNFTInfoId(nftInfoId))
            {
                _logger.Debug("AddOrUpdateNftInfoNewByIdAsync nftInfoId is not common nft {NFTInfoId}", nftInfoId);
                return;
            }

            await AddOrUpdateNftInfoNewAsync(null, nftInfoId, chainId);
        }

        public async Task<NFTInfoNewIndex> AddOrUpdateNftInfoNewAsync(NFTInfoIndex fromNFTInfo, string nftInfoId,
            string chainId)
        {
            var localNFTInfo = await _nftInfoNewIndexRepository.GetAsync(nftInfoId);

            NFTInfoNewIndex nftInfo;

            var changeFlag = false;
            if (localNFTInfo == null)
            {
                if (fromNFTInfo == null)
                {
                    fromNFTInfo = await _graphQlProvider.GetSyncNftInfoRecordAsync(nftInfoId, chainId);
                }

                if (fromNFTInfo == null)
                {
                    _logger.LogError("AddOrUpdateNftInfoNewAsync fromNFTInfo and localNFTInfo are null!");
                    return null;
                }

                nftInfo = _objectMapper.Map<NFTInfoIndex, NFTInfoNewIndex>(fromNFTInfo);
                nftInfo.CountedFlag = FTHelper.IsGreaterThanEqualToOne(nftInfo.Supply, nftInfo.Decimals);
                changeFlag = true;
                if (nftInfo?.ExternalInfoDictionary != null && !nftInfo.ExternalInfoDictionary.IsNullOrEmpty())
                {
                    nftInfo.TraitPairsDictionary = new List<ExternalInfoDictionary>();
                    foreach (var item in nftInfo.ExternalInfoDictionary)
                    {
                        if (item.Key == CommonConstant.NFT_ExternalInfo_Metadata_Key)
                        {
                            var metadata = JsonConvert.DeserializeObject<List<ExternalInfoDictionary>>(item.Value);
                            nftInfo.TraitPairsDictionary.AddRange(metadata);
                            break;
                        }
                    }

                    nftInfo.Generation = nftInfo.TraitPairsDictionary.Count;
                }
            }
            else
            {
                nftInfo = localNFTInfo;
            }

            var checkFlag = await CheckOrUpdateNFTOtherInfoAsync(nftInfo);
            if (checkFlag)
            {
                changeFlag = checkFlag;
            }

            if (changeFlag)
            {
                await _nftInfoNewIndexRepository.AddOrUpdateAsync(nftInfo);
            }

            return nftInfo;
        }

        private async Task<bool> CheckOrUpdateNFTOtherInfoAsync(NFTInfoNewIndex nftInfoNewIndex)
        {
            var changeFlag = false;
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
                var checkFlag = UpdateMinListingInfo(nftInfoNewIndex, listingDto.Items[CommonConstant.IntZero]);
                if (checkFlag)
                {
                    changeFlag = true;
                }
            }
            
            
            var indexerNFTOffer = await _nftOfferProvider.GetMaxOfferInfoAsync(nftInfoNewIndex.Id);
            if (indexerNFTOffer != null && !indexerNFTOffer.Id.IsNullOrEmpty())
            {
                var checkFlag = UpdateMaxOfferInfo(nftInfoNewIndex, indexerNFTOffer);
                if (checkFlag)
                {
                    changeFlag = true;
                }
            }

            return changeFlag;
        } 
        
        private bool UpdateMinListingInfo(NFTInfoNewIndex nftInfoIndex, IndexerNFTListingInfo listingDto)
        {
            var changeFlag = nftInfoIndex.ListingId != listingDto.Id;
            nftInfoIndex.ListingId = listingDto.Id;
            nftInfoIndex.ListingPrice = listingDto.Prices;
            nftInfoIndex.ListingAddress = listingDto.Owner;
            nftInfoIndex.ListingQuantity = listingDto.RealQuantity;
            nftInfoIndex.ListingEndTime = listingDto.ExpireTime;
            nftInfoIndex.LatestListingTime = listingDto.StartTime;
            nftInfoIndex.ListingToken = _objectMapper.Map<IndexerTokenInfo, TokenInfoIndex>(listingDto.PurchaseToken);
            return changeFlag;
        }
        private bool UpdateMaxOfferInfo(NFTInfoNewIndex nftInfoIndex, IndexerNFTOffer indexerNFTOffer)
        {
            var changeFlag = nftInfoIndex.MaxOfferId != indexerNFTOffer.Id;
            nftInfoIndex.MaxOfferId = indexerNFTOffer.Id;
            nftInfoIndex.MaxOfferPrice = indexerNFTOffer.Price;
            nftInfoIndex.MaxOfferExpireTime = indexerNFTOffer.ExpireTime;
            nftInfoIndex.OfferToken = new TokenInfoIndex
            {
                ChainId = indexerNFTOffer.PurchaseToken.ChainId,
                Symbol = indexerNFTOffer.PurchaseToken.Symbol,
                Decimals = Convert.ToInt32(indexerNFTOffer.PurchaseToken.Decimals),
            };
            return changeFlag;
        }

        public async Task<NFTForSaleDto> GetNFTForSaleAsync(GetNFTForSaleInput input)
        {
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
            Dictionary<string, IndexerNFTOffer> maxOfferDict)
        {
            maxOfferDict.TryGetValue(seedSymbolIndex.Id, out var maxOffer);

            var (temDescription, temPrice) = seedSymbolIndex.GetDescriptionAndPrice(maxOffer?.Price ?? 0);

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
                ChainIdStr = seedSymbolIndex.ChainId
            };
        }

        private static CompositeNFTInfoIndexDto MapForNftBriefInfoDto(NFTInfoIndex nftInfoIndex,
            Dictionary<string, IndexerNFTOffer> maxOfferDict)
        {
            maxOfferDict.TryGetValue(nftInfoIndex.Id, out var maxOffer);

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
                //IssueChainId = nftInfoIndex.IssueChainId,
                IssueChainIdStr = ChainHelper.ConvertChainIdToBase58(nftInfoIndex.IssueChainId),
                //ChainId = ChainHelper.ConvertBase58ToChainId(nftInfoIndex.ChainId),
                ChainIdStr = nftInfoIndex.ChainId
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
                        ItemsNumber = nftOwners.IndexerNftUserBalances[i].Amount
                    });
                _logger.LogInformation("GetNFTOwnersAsync-Add owner, address: {id}, cnt: {cnd}",
                    userInfo.Address, nftOwners.IndexerNftUserBalances[i].Amount);
            }

            ret.TotalCount = nftOwners.TotalCount;
            ret.Items = owners;

            return ret;
        }

        private async Task<long> QueryRealCountAsync(
            List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>> mustQuery)
        {
            var countRequest = new SearchRequest<NFTInfoIndex>
            {
                Query = new BoolQuery
                {
                    Must = mustQuery
                        .Select(func => func(new QueryContainerDescriptor<NFTInfoNewIndex>()))
                        .ToList()
                        .AsEnumerable()
                },
                Size = 0
            };

            Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer> queryFunc = q => countRequest.Query;
            var realCount = await _nftInfoNewIndexRepository.CountAsync(queryFunc);
            return realCount.Count;
        }
    }
}