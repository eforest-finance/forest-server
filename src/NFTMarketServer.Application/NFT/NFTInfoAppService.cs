using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Orleans;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
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
        private readonly ISeedInfoProvider _seedInfoProvider;
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
        private readonly IInscriptionProvider _inscriptionProvider;


        public NFTInfoAppService(
            ITokenAppService tokenAppService, IUserAppService userAppService,
            INFTCollectionProvider nftCollectionProvider,
            INFTInfoProvider nftInfoProvider,
            ISeedInfoProvider seedInfoProvider,
            IClusterClient clusterClient,
            ILogger<NFTInfoAppService> logger, IDistributedEventBus distributedEventBus,
            IObjectMapper objectMapper, INFTInfoExtensionProvider nftInfoExtensionProvider,
            INESTRepository<NFTInfoIndex, string> nftInfoIndexRepository,
            ISeedSymbolSyncedProvider seedSymbolSyncedProvider, INFTInfoSyncedProvider nftInfoSyncedProvider,
            INFTOfferProvider nftOfferProvider, 
            INFTListingProvider nftListingProvider,
            IInscriptionProvider inscriptionProvider,
            INFTListingWhitelistPriceProvider nftListingWhitelistPriceProvider)
        {
            _tokenAppService = tokenAppService;
            _userAppService = userAppService;
            _clusterClient = clusterClient;
            _nftInfoProvider = nftInfoProvider;
            _seedInfoProvider = seedInfoProvider;
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
            _inscriptionProvider = inscriptionProvider;
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
            var tick = SymbolHelper.GainInscriptionInfoTick(nftInfoIndex.Symbol);
            var inscriptionInfoDto = await _inscriptionProvider.GetIndexerInscriptionInfoAsync(nftInfoIndex.ChainId, tick);
            nftInfoIndexDto.InscriptionInfo = inscriptionInfoDto;

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
    }
}