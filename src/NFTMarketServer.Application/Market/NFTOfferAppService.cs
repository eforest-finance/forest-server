using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Helper;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Dto;
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
        private readonly INFTCollectionProvider _nftCollectionProvider;
        private readonly INFTCollectionExtensionProvider _nftCollectionExtensionProvider;
        private readonly IUserBalanceProvider _userBalanceProvider;
        private readonly INFTActivityAppService _nftActivityAppService;
        private readonly IObjectMapper _objectMapper;
        
        public NFTOfferAppService(IUserAppService userAppService, INFTOfferProvider nftOfferProvider
                ,INFTCollectionProvider nftCollectionProvider,
                INFTCollectionExtensionProvider nftCollectionExtensionProvider,
                IUserBalanceProvider userBalanceProvider,
                INFTActivityAppService nftActivityAppService,
                IObjectMapper objectMapper)
        {
            _userAppService = userAppService;
            _nftOfferProvider = nftOfferProvider;
            _nftCollectionProvider = nftCollectionProvider;
            _nftCollectionExtensionProvider = nftCollectionExtensionProvider;
            _userBalanceProvider = userBalanceProvider;
            _nftActivityAppService = nftActivityAppService;
            _objectMapper = objectMapper;
        }

        public async Task<PagedResultDto<NFTOfferDto>> GetNFTOffersAsync(GetNFTOffersInput input)
        {
            if (input.SkipCount < 0)
                return buildInitNFTOffersDto();

            var nftOfferIndexes =
                await _nftOfferProvider.GetNFTOfferIndexesAsync(input.SkipCount, input.MaxResultCount,
                    input.ChainId, input.NFTInfoId);
            if (nftOfferIndexes == null || nftOfferIndexes.TotalRecordCount==0)
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

        public async Task<PagedResultDto<CollectedCollectionOffersMadeDto>> GetCollectedCollectionOffersMadeAsync(
            GetCollectedCollectionOffersMadeInput input)
        {
            input.Address = FullAddressHelper.ToShortAddress(input.Address);

            var result = PagedResultWrapper<CollectedCollectionOffersMadeDto>.Initialize();

            var userBalanceList =
                await _userBalanceProvider.GetNFTIdListByUserBalancesAsync(input.CollectionIdList, input.Address,
                    input.ChainList, CommonConstant.IntZero,
                    CommonConstant.IntOneThousand, input.SearchParam);
            if (userBalanceList == null)
            {
                return result;
            }

            var nftInfoIdList = userBalanceList.Item2?.Select(item => item.NFTInfoId).ToList();
            if (nftInfoIdList.IsNullOrEmpty())
            {
                return result;
            }

            var userBalancedic = userBalanceList.Item2?.ToDictionary(item => item.NFTInfoId,
                item => FTHelper.GetIntegerDivision(item.Amount, item.Decimals));


            var activityListRequestDto = BuildGetCollectedActivityListDto(input.CollectionIdList, input.ChainList,
                input.Address, CommonConstant.IntOneThousand, nftInfoIdList, CommonConstant.IntZero, "",
                new List<NFTActivityType>()
                {
                    NFTActivityType.MakeOffer
                });

            var basicResult = await _nftActivityAppService.GetCollectedActivityListAsync(activityListRequestDto);

            var nftCollectionExtensionDic =
                await _nftCollectionExtensionProvider.GetNFTCollectionExtensionsAsync(nftInfoIdList
                    .Select(item => SymbolHelper.TransferNFTIdToCollectionId(item)).ToList());

            var addresses = new List<string>();
            foreach (var info in basicResult.Item2)
            {
                if (!info.From.IsNullOrWhiteSpace())
                    addresses.Add(info.From);
                if (!info.To.IsNullOrWhiteSpace())
                    addresses.Add(info.To);
            }

            var accounts = await _userAppService.GetAccountsAsync(addresses.Distinct().ToList());

            return Map(result, userBalancedic, basicResult, nftCollectionExtensionDic, accounts);
        }

        private PagedResultDto<CollectedCollectionOffersMadeDto> Map(
            PagedResultDto<CollectedCollectionOffersMadeDto> result, Dictionary<string, long> userBalancedic,
            Tuple<long, List<NFTActivityIndex>> basicResult,
            Dictionary<string, NFTCollectionExtensionIndex> nftCollectionExtensionDic,
            Dictionary<string, AccountDto> accounts)
        {
            result.TotalCount = basicResult.Item1;

            basicResult.Item2.Select(index =>
            {
                var dto = _objectMapper.Map<NFTActivityIndex, CollectedCollectionOffersMadeDto>(index);

                if (!index.From.IsNullOrWhiteSpace() && accounts.ContainsKey(index.From))
                    dto.From = index.From.IsNullOrWhiteSpace() ? null : accounts[index.From];

                if (!index.To.IsNullOrWhiteSpace() && accounts.ContainsKey(index.To))
                    dto.To = index.To.IsNullOrWhiteSpace() ? null : accounts[index.To];
                var collectionId = SymbolHelper.TransferNFTIdToCollectionId(index.NftInfoId);
                if (nftCollectionExtensionDic[collectionId] == null)
                {
                    return dto;
                }
                dto.FloorPrice = nftCollectionExtensionDic[collectionId] .FloorPrice;
                dto.FloorPriceSymbol = nftCollectionExtensionDic[collectionId] .FloorPriceSymbol;

                return dto;
            }).ToList();
            
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