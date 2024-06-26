using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Helper;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Market
{
    [RemoteService(IsEnabled = false)]
    public class NFTListingAppService : NFTMarketServerAppService, INFTListingAppService
    {
        private readonly ILogger<NFTListingAppService> _logger;
        private readonly IUserAppService _userAppService;
        private readonly INFTListingProvider _nftListingProvider;
        private readonly IObjectMapper _objectMapper;
        private readonly ICompositeNFTProvider _compositeNFTProvider;

        public NFTListingAppService(IUserAppService userAppService,
            INFTListingProvider nftListingProvider,
            ILogger<NFTListingAppService> logger, 
            IObjectMapper objectMapper,
                ICompositeNFTProvider compositeNFTProvider)
        {
            _userAppService = userAppService;
            _nftListingProvider = nftListingProvider;
            _logger = logger;
            _objectMapper = objectMapper;
            _compositeNFTProvider = compositeNFTProvider;
        }

        public async Task<PagedResultDto<NFTListingIndexDto>> GetNFTListingsAsync(GetNFTListingsInput input)
        {
            try
            {
                var getNftListingsDto = _objectMapper.Map<GetNFTListingsInput, GetNFTListingsDto>(input);
                var listingDto = await _nftListingProvider.GetNFTListingsAsync(getNftListingsDto);

                var listingOwner = listingDto.Items.Select(i => i?.Owner ?? "").ToList();
                var collectionCreator =
                    listingDto.Items.Select(i => i?.NftCollectionDto?.CreatorAddress ?? "").ToList();
                
                // query account info by account address
                var addresses = listingOwner
                    .Concat(collectionCreator)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct().ToList();
                var accountDict = await _userAppService.GetAccountsAsync(addresses);

                var res = listingDto.Items.Select(i =>
                {
                    i.WhitelistId = i.WhitelistId;
                    var item = _objectMapper.Map<IndexerNFTListingInfo, NFTListingIndexDto>(i);
                    item.Owner = accountDict.GetValueOrDefault(i.Owner, new AccountDto(i.Owner))?.WithChainIdAddress(item.ChainId);
                    item.PurchaseToken = _objectMapper.Map<IndexerTokenInfo, TokenDto>(i.PurchaseToken);
                    //item.NFTInfo = _objectMapper.Map<IndexerNFTInfo, NFTImmutableInfoDto>(i.NftInfo);
                    if (item.NFTInfo == null) return item;
                    
                    item.NFTInfo.NftCollection =
                        _objectMapper.Map<IndexerNFTCollection, NFTCollectionIndexDto>(i.NftCollectionDto);
                    if (!i.NftCollectionDto?.CreatorAddress?.IsNullOrEmpty() ?? false)
                    {
                        item.NFTInfo.NftCollection.Creator = accountDict.GetValueOrDefault(
                            i.NftCollectionDto.CreatorAddress,
                            new AccountDto(i.NftCollectionDto.CreatorAddress))?
                            .WithChainIdAddress(i.NftCollectionDto.ChainId);
                    }

                    return item;
                }).ToList();

                return new PagedResultDto<NFTListingIndexDto>
                {
                    Items = res,
                    TotalCount = listingDto.TotalCount
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetNFTListingsAsync ERROR");
                throw new UserFriendlyException("Internal error, please try again later.");
            }
        }

        public async Task<PagedResultDto<CollectedCollectionListingDto>> GetCollectedCollectionListingAsync(
            GetCollectedCollectionListingsInput input)
        {
            input.Address = FullAddressHelper.ToShortAddress(input.Address);
            
            var nftInfoIds = new List<string>();
            if (!input.SearchParam.IsNullOrEmpty() || !input.CollectionIdList.IsNullOrEmpty())
            {
                var compositeNFTDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(input.CollectionIdList,
                    input.SearchParam, CommonConstant.IntZero, CommonConstant.IntOneThousand);
                nftInfoIds = compositeNFTDic?.Keys.ToList();
                if (nftInfoIds.IsNullOrEmpty())
                {
                    return new PagedResultDto<CollectedCollectionListingDto>()
                    {
                        TotalCount = CommonConstant.IntZero,
                        Items = new List<CollectedCollectionListingDto>()
                    };
                }
            }

            var collectedNFTListings = await _nftListingProvider.GetCollectedNFTListingsAsync(input.SkipCount,
                input.MaxResultCount, input.Address, input.ChainList, nftInfoIds);

            if (collectedNFTListings == null || collectedNFTListings.TotalRecordCount == CommonConstant.IntZero)
            {
                return new PagedResultDto<CollectedCollectionListingDto>()
                {
                    TotalCount = CommonConstant.IntZero,
                    Items = new List<CollectedCollectionListingDto>()
                };
            }
            
            var nftInfoIdList = collectedNFTListings.IndexerNFTListingInfoList?.Select(item => item.BusinessId).ToList();

            var compositeNFTInfoDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(nftInfoIdList);
            
            var listingOwner = collectedNFTListings.IndexerNFTListingInfoList?.Select(i => i?.Owner ?? "").ToList();
            
            var addresses = listingOwner
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct().ToList();
            var accountDict = await _userAppService.GetAccountsAsync(addresses);

            var res = collectedNFTListings.IndexerNFTListingInfoList.Select(i =>
            {
                var item = _objectMapper.Map<IndexerNFTListingInfo, CollectedCollectionListingDto>(i);
                item.Owner = accountDict.GetValueOrDefault(i.Owner, new AccountDto(i.Owner))
                    ?.WithChainIdAddress(item.ChainId);
                item.PurchaseToken = _objectMapper.Map<IndexerTokenInfo, TokenDto>(i.PurchaseToken);
                if (compositeNFTInfoDic.ContainsKey(i.BusinessId) && compositeNFTInfoDic[i.BusinessId] != null)
                {
                    item.PreviewImage = compositeNFTInfoDic[i.BusinessId].PreviewImage;
                    item.NFTName = compositeNFTInfoDic[i.BusinessId].NFTName;
                    item.CollectionName = compositeNFTInfoDic[i.BusinessId].CollectionName;
                    item.Decimals = compositeNFTInfoDic[i.BusinessId].Decimals;
                    item.Prices = FTHelper.ToPrice(item.Prices, item.Decimals);
                    item.NFTSymbol = compositeNFTInfoDic[i.BusinessId].Symbol;
                }

                return item;
            }).ToList();

            return new PagedResultDto<CollectedCollectionListingDto>
            {
                Items = res,
                TotalCount = collectedNFTListings.TotalRecordCount
            };
        }
    }
    
    
}