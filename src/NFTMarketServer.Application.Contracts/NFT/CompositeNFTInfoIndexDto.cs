using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NFTMarketServer.Users;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public class CompositeNFTInfoIndexDto : EntityDto<string>
    {
        public string CollectionSymbol { get; set; }
        public string NFTSymbol { get; set; }
        [CanBeNull] public string PreviewImage { get; set; }
        [CanBeNull] public string PriceDescription { get; set; }
        public decimal? Price { get; set; }
        
        public string TokenName { get; set; }
        public string IssueChainIdStr { get; set; }
        public string ChainIdStr { get; set; }
        public string fileExtension { get; set; }
        
        public int Generation { get; set; } = -1;
        
        public List<MetadataDto> TraitPairsDictionary { get; set; }
        
        public decimal ListingPrice  { get; set; }

        public DateTime? ListingPriceCreateTime { get; set; }

        public decimal OfferPrice  { get; set; }

        public decimal LatestDealPrice  { get; set; }

        public long AllOwnerCount  { get; set; }

        public AccountDto RealOwner { get; set; }
    }
}