using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Helper;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Dto;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Users;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Market
{
    [RemoteService(IsEnabled = false)]
    public class NFTOfferAppService : NFTMarketServerAppService, INFTOfferAppService
    {
        private readonly IUserAppService _userAppService;
        private readonly INFTOfferProvider _nftOfferProvider;
        private readonly INFTCollectionExtensionProvider _nftCollectionExtensionProvider;
        private readonly IUserBalanceProvider _balanceProvider;
        private readonly ICompositeNFTProvider _compositeNFTProvider;
        private const int MaxQueryOfferParamSize = 200;
        private readonly IObjectMapper _objectMapper;
        private readonly ILogger<NFTOfferAppService> _logger;
        
        public NFTOfferAppService(IUserAppService userAppService, INFTOfferProvider nftOfferProvider,
        INFTCollectionExtensionProvider nftCollectionExtensionProvider,
                IUserBalanceProvider balanceProvider,
                ICompositeNFTProvider compositeNFTProvider,
                IObjectMapper objectMapper,
                ILogger<NFTOfferAppService> logger)
        {
            _userAppService = userAppService;
            _nftOfferProvider = nftOfferProvider;
            _nftCollectionExtensionProvider = nftCollectionExtensionProvider;
            _balanceProvider = balanceProvider;
            _compositeNFTProvider = compositeNFTProvider;
            _objectMapper = objectMapper;
            _logger = logger;
        }

        public async Task<PagedResultDto<NFTOfferDto>> GetNFTOffersAsync(GetNFTOffersInput input)
        {
            if (input.SkipCount < 0)
                return buildInitNFTOffersDto();

            var nftOfferIndexes =
                await _nftOfferProvider.GetNFTOfferIndexesAsync(input.SkipCount, input.MaxResultCount,
                    input.ChainId, new List<string>(), input.NFTInfoId, new List<string>(),string.Empty, string.Empty,input.ExcludeAddress);
            if (nftOfferIndexes == null || nftOfferIndexes.TotalRecordCount==0 || nftOfferIndexes.IndexerNFTOfferList.IsNullOrEmpty())
                return buildInitNFTOffersDto();
            
            var totalCount = nftOfferIndexes.TotalRecordCount;
            if (nftOfferIndexes.IndexerNFTOfferList == null)
                return buildInitNFTOffersDto();

            var addresses = new List<string>();
            foreach (var info in nftOfferIndexes.IndexerNFTOfferList)
            {
                if (!info.From.IsNullOrWhiteSpace())
                    addresses.Add(info.From);
                if (!info.To.IsNullOrWhiteSpace())
                    addresses.Add(info.To);
            }

            var accounts = await _userAppService.GetAccountsAsync(addresses.Distinct().ToList());

            var nftCollectionExtensionIndexId = SymbolHelper.TransferNFTIdToCollectionId(input.NFTInfoId);
            NFTCollectionExtensionIndex nftCollectionExtension = null;
            if (!nftCollectionExtensionIndexId.IsNullOrWhiteSpace())
            {
                nftCollectionExtension = await _nftCollectionExtensionProvider.GetNFTCollectionExtensionAsync(nftCollectionExtensionIndexId);
            }

            var result = nftOfferIndexes.IndexerNFTOfferList.Select(o => MapForIndexerNFTOffer(o, accounts,
                nftCollectionExtension)).ToList();
            return new PagedResultDto<NFTOfferDto>
            {
                Items = result,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResultDto<CollectedCollectionOffersDto>> GetCollectedCollectionOffersMadeAsync(
            GetCollectedCollectionOffersMadeInput input)
        {
            input.Address = FullAddressHelper.ToShortAddress(input.Address);

            var result = PagedResultWrapper<CollectedCollectionOffersDto>.Initialize();

            var nftInfoIds = new List<string>();
            if (!input.SearchParam.IsNullOrEmpty() || !input.CollectionIdList.IsNullOrEmpty())
            {
                int skip = CommonConstant.IntZero;
                var compositeNFTDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(input.CollectionIdList,
                    input.SearchParam, skip, CommonConstant.IntOneThousand);
                nftInfoIds = compositeNFTDic?.Keys.ToList();
                while (nftInfoIds.Count >= CommonConstant.IntOneThousand)
                {
                     skip += CommonConstant.IntOneThousand;
                     compositeNFTDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(input.CollectionIdList,
                         input.SearchParam, skip, CommonConstant.IntOneThousand);
                     var infoIds = compositeNFTDic?.Keys.ToList();
                     if (infoIds.IsNullOrEmpty())
                     {
                         break;
                     }
                     
                     {
                         nftInfoIds.AddRange(infoIds);
                     }
                 }
                if (nftInfoIds.IsNullOrEmpty())
                {
                    return result;
                }
            }
            
            
            var nftOfferIndexes =
                await _nftOfferProvider.GetNFTOfferIndexesAsync(input.SkipCount, input.MaxResultCount,
                    string.Empty, input.ChainList.IsNullOrEmpty()?new List<string>():input.ChainList, string.Empty, nftInfoIds, input.Address, string.Empty, string.Empty);
            if (nftOfferIndexes == null || nftOfferIndexes.IndexerNFTOfferList.IsNullOrEmpty())
            {
                return result;
            }
            
            var nftInfoIdList = nftOfferIndexes.IndexerNFTOfferList?.Select(item => item.BizInfoId).ToList();
            
            var compositeNFTInfoDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(nftInfoIdList);

            var nftCollectionExtensionDic =
                await _nftCollectionExtensionProvider.GetNFTCollectionExtensionsAsync(nftInfoIdList
                    .Select(item => SymbolHelper.TransferNFTIdToCollectionId(item)).ToList());

            var addresses = new List<string>();
            foreach (var info in nftOfferIndexes.IndexerNFTOfferList)
            {
                if (!info.From.IsNullOrWhiteSpace())
                    addresses.Add(info.From);
                if (!info.To.IsNullOrWhiteSpace())
                    addresses.Add(info.To);
            }

            var accounts = await _userAppService.GetAccountsAsync(addresses.Distinct().ToList());

            return Map(result, nftOfferIndexes, nftCollectionExtensionDic, accounts, compositeNFTInfoDic);
        }

        public async Task<PagedResultDto<CollectedCollectionOffersDto>> GetCollectedCollectionReceivedOfferAsync(GetCollectedCollectionReceivedOfferInput input)
        {
            input.Address = FullAddressHelper.ToShortAddress(input.Address);

            var result = PagedResultWrapper<CollectedCollectionOffersDto>.Initialize();

            var nftInfoIds = new List<string>();
            if (!input.SearchParam.IsNullOrEmpty() || !input.CollectionIdList.IsNullOrEmpty())
            {
                int skip = CommonConstant.IntZero;
                var compositeNFTDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(input.CollectionIdList,
                    input.SearchParam, skip, CommonConstant.IntOneThousand);
                nftInfoIds = compositeNFTDic?.Keys.ToList();
                while (nftInfoIds.Count >= CommonConstant.IntOneThousand)
                {
                    skip += CommonConstant.IntOneThousand;
                    compositeNFTDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(input.CollectionIdList,
                        input.SearchParam, skip, CommonConstant.IntOneThousand);
                    var infoIds = compositeNFTDic?.Keys.ToList();
                    if (infoIds.IsNullOrEmpty())
                    {
                        break;
                    }
                    nftInfoIds.AddRange(infoIds);
                }
                
            }
            else
            {
                var userBalance = await _balanceProvider.GetNFTIdListByUserBalancesAsync(input.CollectionIdList,
                    input.Address, input.ChainList, CommonConstant.IntZero, CommonConstant.IntOneThousand,
                    string.Empty);
                if (userBalance != null && !userBalance.Item2.IsNullOrEmpty())
                {
                    nftInfoIds.AddRange(userBalance.Item2.Select(item=>item.NFTInfoId).ToList());
                }
                
            }

            if (nftInfoIds.IsNullOrEmpty())
            {
                return result;
            }
            
            var offers = new List<IndexerNFTOffer>();
            if(!nftInfoIds.IsNullOrEmpty())
            {
                var groupInfoIds = GroupInfoIds(nftInfoIds);
                foreach (var infoIds in groupInfoIds)
                {
                    var subResult = await _nftOfferProvider.GetNFTOfferIndexesAsync(input.SkipCount, input.MaxResultCount,
                        string.Empty, input.ChainList.IsNullOrEmpty()?new List<string>():input.ChainList, string.Empty, infoIds, string.Empty, string.Empty, input.Address);
                    if (subResult != null && subResult.TotalRecordCount > 0 &&
                        !subResult.IndexerNFTOfferList.IsNullOrEmpty())
                    {
                        offers.AddRange(subResult.IndexerNFTOfferList);
                    }

                }
            }
            var nftOfferIndexes = new IndexerNFTOffers()
            {
                TotalRecordCount = offers.Count,
                IndexerNFTOfferList = offers
            };


            if (nftOfferIndexes == null || nftOfferIndexes.IndexerNFTOfferList.IsNullOrEmpty())
            {
                return result;
            }
            
            var nftInfoIdList = nftOfferIndexes.IndexerNFTOfferList?.Select(item => item.BizInfoId).ToList();

            var compositeNFTInfoDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(nftInfoIdList);
            
            var nftCollectionExtensionDic =
                await _nftCollectionExtensionProvider.GetNFTCollectionExtensionsAsync(nftInfoIdList
                    .Select(item => SymbolHelper.TransferNFTIdToCollectionId(item)).ToList());

            var addresses = new List<string>();
            foreach (var info in nftOfferIndexes.IndexerNFTOfferList)
            {
                if (!info.From.IsNullOrWhiteSpace())
                    addresses.Add(info.From);
                if (!info.To.IsNullOrWhiteSpace())
                    addresses.Add(info.To);
            }

            var accounts = await _userAppService.GetAccountsAsync(addresses.Distinct().ToList());

            return Map(result, nftOfferIndexes, nftCollectionExtensionDic, accounts, compositeNFTInfoDic);
        }
        
        private static IEnumerable<List<string>> GroupInfoIds(List<string> originalList)
        {
            const int groupPageCount = 100;
            var groupedList = new List<List<string>>();
 
            for (var i = 0; i < originalList.Count; i += groupPageCount)
            {
                var count = Math.Min(100, originalList.Count - i); 
                var subList = originalList.GetRange(i, count);
                groupedList.Add(subList);
            }

            return groupedList;
        }
        private PagedResultDto<CollectedCollectionOffersDto> Map(
            PagedResultDto<CollectedCollectionOffersDto> result,
            IndexerNFTOffers nftOfferIndexes,
            Dictionary<string, NFTCollectionExtensionIndex> nftCollectionExtensionDic,
            Dictionary<string, AccountDto> accounts,
            Dictionary<string, CompositeNFTDto> compositeNFTDic)
        {
            result.TotalCount = nftOfferIndexes.TotalRecordCount;
            result.Items = nftOfferIndexes.IndexerNFTOfferList.Select(index =>
            {
                
                var dto = _objectMapper.Map<IndexerNFTOffer, CollectedCollectionOffersDto>(index);
                
                _logger.LogDebug("CollectedCollectionOffersMadeDto 1 from {A} to {B}",JsonConvert.SerializeObject(index),JsonConvert.SerializeObject(dto));

                if (!index.From.IsNullOrWhiteSpace() && accounts.ContainsKey(index.From))
                    dto.From = index.From.IsNullOrWhiteSpace() ? null : accounts[index.From];

                if (!index.To.IsNullOrWhiteSpace() && accounts.ContainsKey(index.To))
                    dto.To = index.To.IsNullOrWhiteSpace() ? null : accounts[index.To];
                var collectionId = SymbolHelper.TransferNFTIdToCollectionId(index.BizInfoId);
                if (nftCollectionExtensionDic.ContainsKey(collectionId) && nftCollectionExtensionDic[collectionId] != null)
                {
                    dto.FloorPrice = nftCollectionExtensionDic[collectionId] .FloorPrice;
                    dto.FloorPriceSymbol = nftCollectionExtensionDic[collectionId] .FloorPriceSymbol;
                    dto.CollectionName = nftCollectionExtensionDic[collectionId].TokenName;
                    dto.CollectionLogoImage =
                        FTHelper.BuildIpfsUrl(nftCollectionExtensionDic[collectionId].LogoImage);
                }

                if (compositeNFTDic.ContainsKey(index.BizInfoId) && compositeNFTDic[index.BizInfoId]!=null)
                {
                    dto.PreviewImage = compositeNFTDic[index.BizInfoId].PreviewImage;
                    dto.NFTName = compositeNFTDic[index.BizInfoId].NFTName;
                    dto.Decimals = compositeNFTDic[index.BizInfoId].Decimals;
                    dto.NFTSymbol = compositeNFTDic[index.BizInfoId].Symbol;
                    dto.NFTInfoId = compositeNFTDic[index.BizInfoId].NFTInfoId;
                    
                    var quantityNoDecimals = FTHelper.GetIntegerDivision(index.Quantity, dto.Decimals);
                    index.RealQuantity = (quantityNoDecimals == index.RealQuantity)
                        ? index.RealQuantity
                        : Math.Min(index.RealQuantity, quantityNoDecimals);
                    dto.Quantity = index.RealQuantity;
                }
                
                _logger.LogDebug("CollectedCollectionOffersMadeDto 2 from {A} to {B}",JsonConvert.SerializeObject(index),JsonConvert.SerializeObject(dto));

                return dto;
            }).ToList();
            if (result.Items.IsNullOrEmpty())
            {
                result.Items = new List<CollectedCollectionOffersDto>();
            }
            return result;
        }

        private static GetCollectedActivityListDto BuildGetCollectedActivityListDto(List<string> collectionIdList,
            List<string> chainList, string fromAddress, int maxResultCount, List<string> nftInfoIds, int skipCount,
            string toAddress, List<NFTActivityType> typeList)
        {
            return new GetCollectedActivityListDto
            {
                CollectionIdList = collectionIdList,
                ChainList = chainList,
                FromAddress = fromAddress,
                MaxResultCount = maxResultCount,
                NFTInfoIds = nftInfoIds,
                SkipCount = skipCount,
                ToAddress = toAddress,
                TypeList = typeList
            };
        }

        private PagedResultDto<NFTOfferDto> buildInitNFTOffersDto()
        {
            return new PagedResultDto<NFTOfferDto>
            {
                Items = new List<NFTOfferDto>(),
                TotalCount = 0
            };
        }

        private NFTOfferDto MapForIndexerNFTOffer(IndexerNFTOffer index
            , Dictionary<string, AccountDto> accounts,NFTCollectionExtensionIndex nftCollectionExtensionIndex)
        {
            if (index == null)
            {
                return null;
            }

            var offer = ObjectMapper.Map<IndexerNFTOffer, NFTOfferDto>(index);

            if (!index.From.IsNullOrWhiteSpace() && accounts.ContainsKey(index.From))
                offer.From = index.From.IsNullOrWhiteSpace() ? null : accounts[index.From];

            if (!index.To.IsNullOrWhiteSpace() && accounts.ContainsKey(index.To))
                offer.To = index.To.IsNullOrWhiteSpace() ? null : accounts[index.To];

            if (nftCollectionExtensionIndex != null)
            {
                offer.FloorPrice = nftCollectionExtensionIndex.FloorPrice;
                offer.FloorPriceSymbol = nftCollectionExtensionIndex.FloorPriceSymbol;
            }
            
            return offer;
        }
    }
}