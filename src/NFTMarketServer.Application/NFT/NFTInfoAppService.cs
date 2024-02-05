using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.Helper;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
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
        private readonly INFTListingWhitelistPriceProvider _nftListingWhitelistPriceProvider;
        private readonly INESTRepository<NFTInfoIndex, string> _nftInfoIndexRepository;
        private readonly ISeedSymbolSyncedProvider _seedSymbolSyncedProvider;
        private readonly INFTInfoSyncedProvider _nftInfoSyncedProvider;
        private readonly INFTOfferProvider _nftOfferProvider;
        private readonly INFTListingProvider _nftListingProvider;
        private readonly INFTCollectionExtensionProvider _nftCollectionExtensionProvider;
        private readonly INFTDealInfoProvider _nftDealInfoProvider;
        private readonly IInscriptionProvider _inscriptionProvider;
        private readonly NFTCollectionAppService _nftCollectionAppService;
        private readonly IDistributedCache<string> _distributedCache;

        private readonly IOptionsMonitor<ResetNFTSyncHeightExpireMinutesOptions>
            _resetNFTSyncHeightExpireMinutesOptionsMonitor;

        public NFTInfoAppService(
            ITokenAppService tokenAppService, IUserAppService userAppService,
            INFTCollectionProvider nftCollectionProvider,
            INFTInfoProvider nftInfoProvider,
            IClusterClient clusterClient,
            ILogger<NFTInfoAppService> logger, IDistributedEventBus distributedEventBus,
            IObjectMapper objectMapper, INFTInfoExtensionProvider nftInfoExtensionProvider,
            INESTRepository<NFTInfoIndex, string> nftInfoIndexRepository,
            ISeedSymbolSyncedProvider seedSymbolSyncedProvider, INFTInfoSyncedProvider nftInfoSyncedProvider,
            INFTOfferProvider nftOfferProvider,
            INFTListingProvider nftListingProvider,
            INFTDealInfoProvider nftDealInfoProvider,
            IInscriptionProvider inscriptionProvider,
            INFTListingWhitelistPriceProvider nftListingWhitelistPriceProvider,
            INFTCollectionExtensionProvider nftCollectionExtensionProvider,
            NFTCollectionAppService nftCollectionAppService,
            IDistributedCache<string> distributedCache,
            IOptionsMonitor<ResetNFTSyncHeightExpireMinutesOptions> resetNFTSyncHeightExpireMinutesOptionsMonitor)
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
            _nftListingWhitelistPriceProvider = nftListingWhitelistPriceProvider;
            _nftInfoIndexRepository = nftInfoIndexRepository;
            _seedSymbolSyncedProvider = seedSymbolSyncedProvider;
            _nftInfoSyncedProvider = nftInfoSyncedProvider;
            _nftOfferProvider = nftOfferProvider;
            _nftListingProvider = nftListingProvider;
            _nftDealInfoProvider = nftDealInfoProvider;
            _nftCollectionExtensionProvider = nftCollectionExtensionProvider;
            _inscriptionProvider = inscriptionProvider;
            _nftCollectionAppService = nftCollectionAppService;
            _distributedCache = distributedCache;
            _resetNFTSyncHeightExpireMinutesOptionsMonitor = resetNFTSyncHeightExpireMinutesOptionsMonitor;
        }

        public async Task<PagedResultDto<NFTInfoIndexDto>> GetNFTInfosAsync(GetNFTInfosInput input)
        {
            if (!PreCheckGetNFTInfosInput(input))
            {
                return PagedResultWrapper<NFTInfoIndexDto>.Initialize();
            }

            if (input.PriceHigh > 0)
            {
                input.PriceHigh = await GetPriceElfFromUSDAsync(input.PriceHigh, SymbolHelper.CoinGeckoELF());
            }

            if (input.PriceLow > 0)
            {
                input.PriceLow = await GetPriceElfFromUSDAsync(input.PriceLow, SymbolHelper.CoinGeckoELF());
            }

            var pageResult = await _nftInfoProvider.GetNFTInfoIndexsAsync(input.SkipCount,
                input.MaxResultCount,
                input.NFTCollectionId,
                input.Sorting,
                input.PriceLow,
                input.PriceHigh,
                input.Status,
                input.Address,
                input.IssueAddress,
                input.NFTInfoIds
            );

            if (pageResult == null || pageResult.TotalRecordCount == 0)
            {
                return PagedResultWrapper<NFTInfoIndexDto>.Initialize();
            }

            var result = await BuildNFTInfoIndexListAsync(input.Address, pageResult.IndexerNftInfos);
            return new PagedResultDto<NFTInfoIndexDto>
            {
                Items = result,
                TotalCount = pageResult.TotalRecordCount
            };
        }

        public async Task<PagedResultDto<UserProfileNFTInfoIndexDto>> GetNFTInfosForUserProfileAsync(
            GetNFTInfosProfileInput input)
        {
            //query nft infos
            var nftInfos = await _nftInfoSyncedProvider.GetNFTInfosUserProfileAsync(input);
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
                TotalCount = totalRecordCount
            };
        }

        public async Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetCompositeNFTInfosAsync(
            GetCompositeNFTInfosInput input)
        {
            var result = PagedResultWrapper<CompositeNFTInfoIndexDto>.Initialize();

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
                var nftResult = await _nftInfoSyncedProvider.GetNFTBriefInfosAsync(input);

                var maxOfferDict = await GetMaxOfferInfosAsync(nftResult.Item2.Select(info => info.Id).ToList());

                result = new PagedResultDto<CompositeNFTInfoIndexDto>()
                {
                    TotalCount = nftResult.Item1,
                    Items = nftResult.Item2.Select(item => MapForNftBriefInfoDto(item, maxOfferDict)).ToList()
                };
            }

            try
            {
                var compositeNFTInfoIndexDto = result?.Items?.FirstOrDefault();
                if (compositeNFTInfoIndexDto == null || compositeNFTInfoIndexDto.TokenName.IsNullOrEmpty())
                {
                    return await MapForCompositeNftInfoIndexDtoPage(result);
                }

                var checkInput = new SearchNFTCollectionsInput()
                {
                    TokenName = compositeNFTInfoIndexDto.TokenName
                };
                var collectionResult = await _nftCollectionAppService.SearchNFTCollectionsAsync(checkInput);
                var searchNftCollectionsDto = collectionResult?.Items?.FirstOrDefault();

                if (searchNftCollectionsDto == null ||
                    searchNftCollectionsDto.ItemTotal == result.TotalCount)
                {
                    return await MapForCompositeNftInfoIndexDtoPage(result);
                }

                var resetSyncHeightFlag =
                    await _distributedCache.GetAsync(CommonConstant.ResetNFTSyncHeightFlagCacheKey);
                _logger.Debug("GetCompositeNFTInfosAsync origin {ResetSyncHeightFlag} {resetNftSyncHeightExpireMinutes}",
                    resetSyncHeightFlag,
                    _resetNFTSyncHeightExpireMinutesOptionsMonitor?.CurrentValue.ResetNFTSyncHeightExpireMinutes);
                if (resetSyncHeightFlag.IsNullOrEmpty())
                {
                    var resetNftSyncHeightExpireMinutes =
                        _resetNFTSyncHeightExpireMinutesOptionsMonitor?.CurrentValue.ResetNFTSyncHeightExpireMinutes ??
                        CommonConstant.CacheExpirationMinutes;
                    await _distributedCache.SetAsync(CommonConstant.ResetNFTSyncHeightFlagCacheKey,
                        CommonConstant.ResetNFTSyncHeightFlagCacheKey, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(resetNftSyncHeightExpireMinutes)
                        });
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Something is wrong {Message}", e.Message);
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

            var nftInfoIndex = await _nftInfoSyncedProvider.GetNFTInfoIndexAsync(input.Id);
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
                await FillWhitelistPriceAsync(new List<NFTInfoIndexDto> { nftInfoIndexDto }, input.Address);
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

            var tick = SymbolHelper.GainInscriptionInfoTick(nftInfoIndex.Symbol);
            var inscriptionInfoDto =
                await _inscriptionProvider.GetIndexerInscriptionInfoAsync(nftInfoIndex.ChainId, tick);
            nftInfoIndexDto.InscriptionInfo = inscriptionInfoDto;

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

            if (!address.IsNullOrWhiteSpace())
            {
                result = await FillWhitelistPriceAsync(result, address);
            }

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

        private async Task<List<NFTInfoIndexDto>> FillWhitelistPriceAsync(List<NFTInfoIndexDto> dtos, string address)
        {
            var nftInfoIds = dtos?.Select(dto => dto.Id).Where(id => !id.IsNullOrEmpty()).ToList() ??
                             new List<string>();
            try
            {
                if (nftInfoIds.IsNullOrEmpty())
                {
                    return new List<NFTInfoIndexDto>();
                }

                // async query graphQL by page
                const int pageSize = 10;
                var pages = nftInfoIds.Select((item, index) => new { Item = item, Index = index })
                    .GroupBy(x => x.Index / pageSize)
                    .Select(g => g.Select(x => x.Item).ToList())
                    .ToList();
                var queryTaskList = pages.Select(ids =>
                    _nftListingWhitelistPriceProvider.GetNFTListingWhitelistPricesAsync(address, nftInfoIds)).ToList();

                var whitelistPrices = new List<IndexerListingWhitelistPrice>();
                foreach (var task in queryTaskList)
                {
                    whitelistPrices.AddRange(await task);
                }

                var whitelistPriceDic = whitelistPrices.ToDictionary(o => o.NftInfoId, o => o);
                foreach (var dto in dtos!)
                {
                    if (whitelistPriceDic.TryGetValue(dto.Id, out var value))
                    {
                        ObjectMapper.Map(value, dto);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "FillWhitelistPriceAsync fail, Address={Address}, NftInfoIds={Join}", address,
                    string.Join(",", nftInfoIds));
            }

            return dtos;
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

        public async Task<NFTForSaleDto> GetNFTForSaleAsync(GetNFTForSaleInput input)
        {
            var nftInfoIndex = await _nftInfoSyncedProvider.GetNFTInfoIndexAsync(input.Id);
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

            var nftInfoIndex = await _nftInfoSyncedProvider.GetNFTInfoIndexAsync(input.Id);
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
    }
}