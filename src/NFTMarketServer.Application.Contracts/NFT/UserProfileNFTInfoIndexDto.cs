using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public class UserProfileNFTInfoIndexDto : EntityDto<string>
    {
        public long NFTTokenId { get; set; }
        public string ChainId { get; set; }
        public string? FileExtension { get; set; }
        public string? PreviewImage { get; set; }
        public bool IsOfficial { get; set; }
        public string? TokenName { get; set; }
        public decimal ListingPrice { get; set; }
        public decimal LatestDealPrice { get; set; }
        public decimal? WhitelistPrice { get; set; }
        public string? ListingAddress { get; set; }
        public bool CanBuyFlag { get; set; }
        public string IssueChainIdStr { get; set; }
        public List<MetadataDto>? Metadata { get; set; }

        public UserProfileNFTCollectionIndexDto? NFTCollection { get; set; }
        public UserProfileTokenDto? ListingToken { get; set; }
        public UserProfileTokenDto?  LatestDealToken  { get; set; }
        public UserProfileTokenDto? WhitelistPriceToken { get; set; }
    }

    
    public class UserProfileNFTCollectionIndexDto : EntityDto<string>
    {
        public string TokenName { get; set; }
    }
    
    public class UserProfileTokenDto : EntityDto<string>
    {
        public string Symbol { get; set; }
    }
   
}