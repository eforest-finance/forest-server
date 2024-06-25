using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        private readonly ILogger<NFTOfferAppService> _logger;
        
        public NFTOfferAppService(IUserAppService userAppService, INFTOfferProvider nftOfferProvider
                ,INFTCollectionProvider nftCollectionProvider,
                INFTCollectionExtensionProvider nftCollectionExtensionProvider,
                IUserBalanceProvider userBalanceProvider,
                INFTActivityAppService nftActivityAppService,
                IObjectMapper objectMapper,
                ILogger<NFTOfferAppService> logger)
        {
            _userAppService = userAppService;
            _nftOfferProvider = nftOfferProvider;
            _nftCollectionProvider = nftCollectionProvider;
            _nftCollectionExtensionProvider = nftCollectionExtensionProvider;
            _userBalanceProvider = userBalanceProvider;
            _nftActivityAppService = nftActivityAppService;
            _objectMapper = objectMapper;
            _logger = logger;
        }

        public async Task<PagedResultDto<NFTOfferDto>> GetNFTOffersAsync(GetNFTOffersInput input)
        {
            if (input.SkipCount < 0)
                return buildInitNFTOffersDto();

            var nftOfferIndexes =
                await _nftOfferProvider.GetNFTOfferIndexesAsync(input.SkipCount, input.MaxResultCount,
                    input.ChainId, new List<string>(), input.NFTInfoId, "", "");
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

            var nftOfferIndexes =
                await _nftOfferProvider.GetNFTOfferIndexesAsync(input.SkipCount, input.MaxResultCount,
                    "", input.ChainList, "", input.Address, "");
            if (nftOfferIndexes == null || nftOfferIndexes.IndexerNFTOfferList.IsNullOrEmpty())
            {
                return result;
            }
            
            var nftInfoIdList = nftOfferIndexes.IndexerNFTOfferList?.Select(item => item.BizInfoId).ToList();

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

            return Map(result, nftOfferIndexes, nftCollectionExtensionDic, accounts);
        }

        private PagedResultDto<CollectedCollectionOffersMadeDto> Map(
            PagedResultDto<CollectedCollectionOffersMadeDto> result,
            IndexerNFTOffers nftOfferIndexes,
            Dictionary<string, NFTCollectionExtensionIndex> nftCollectionExtensionDic,
            Dictionary<string, AccountDto> accounts)
        {
            result.TotalCount = nftOfferIndexes.TotalRecordCount;

            nftOfferIndexes.IndexerNFTOfferList.Select(index =>
            {
                
                var dto = _objectMapper.Map<IndexerNFTOffer, CollectedCollectionOffersMadeDto>(index);
                _logger.LogDebug("CollectedCollectionOffersMadeDto 1 from {A} to {B}",JsonConvert.SerializeObject(index),JsonConvert.SerializeObject(dto));

                if (!index.From.IsNullOrWhiteSpace() && accounts.ContainsKey(index.From))
                    dto.From = index.From.IsNullOrWhiteSpace() ? null : accounts[index.From];

                if (!index.To.IsNullOrWhiteSpace() && accounts.ContainsKey(index.To))
                    dto.To = index.To.IsNullOrWhiteSpace() ? null : accounts[index.To];
                var collectionId = SymbolHelper.TransferNFTIdToCollectionId(index.BizInfoId);
                if (nftCollectionExtensionDic[collectionId] != null)
                {
                    dto.FloorPrice = nftCollectionExtensionDic[collectionId] .FloorPrice;
                    dto.FloorPriceSymbol = nftCollectionExtensionDic[collectionId] .FloorPriceSymbol;
                    dto.CollectionName = nftCollectionExtensionDic[collectionId].TokenName;
                }
                
                _logger.LogDebug("CollectedCollectionOffersMadeDto 2 from {A} to {B}",JsonConvert.SerializeObject(index),JsonConvert.SerializeObject(dto));

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