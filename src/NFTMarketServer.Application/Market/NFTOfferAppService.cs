using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Users;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Market
{
    [RemoteService(IsEnabled = false)]
    public class NFTOfferAppService : NFTMarketServerAppService, INFTOfferAppService
    {
        private readonly IUserAppService _userAppService;
        private readonly INFTOfferProvider _nftOfferProvider;
        private readonly INFTCollectionProvider _nftCollectionProvider;
        private readonly INFTCollectionExtensionProvider _nftCollectionExtensionProvider;
        
        public NFTOfferAppService(IUserAppService userAppService, INFTOfferProvider nftOfferProvider
                ,INFTCollectionProvider nftCollectionProvider,
                INFTCollectionExtensionProvider nftCollectionExtensionProvider)
        {
            _userAppService = userAppService;
            _nftOfferProvider = nftOfferProvider;
            _nftCollectionProvider = nftCollectionProvider;
            _nftCollectionExtensionProvider = nftCollectionExtensionProvider;
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