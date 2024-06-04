using System;
using System.Collections.Generic;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public class NFTInfoIndexDto : EntityDto<string>
    {
        public string ChainId { get; set; }
        public int IssueChainId { get; set; }
        public string IssueChainIdStr { get; set; }
        public string NFTSymbol { get; set; }
        public long NFTTokenId { get; set; }
        public string? WhitelistId { get; set; }
        public string Issuer { get; set; }
        public string proxyIssuerAddress { get; set; }
        public AccountDto Minter { get; set; }
        public AccountDto Owner { get; set; }
        public long OwnerCount { get; set; }
        public string Uri { get; set; }
        public string? TokenName { get; set; }
        public long TotalQuantity { get; set; }
        public int Decimals { get; set; }
        public bool CanBuyFlag { get; set; }
        public string? ListingId { get; set; }
        public string? ListingAddress { get; set; }
        public decimal ListingPrice { get; set; }
        public long ListingQuantity { get; set; }
        public DateTime? ListingEndTime { get; set; }
        public DateTime? LatestListingTime { get; set; }
        public decimal LatestDealPrice { get; set; }
        public DateTime? LatestDealTime { get; set; }
        public List<MetadataDto>? Metadata { get; set; }
        
        public decimal? WhitelistPrice { get; set; }
        public TokenDto? ListingToken { get; set; }
        public TokenDto?  LatestDealToken  { get; set; }
        public TokenDto? WhitelistPriceToken { get; set; }
        
        public NFTCollectionIndexDto? NFTCollection { get; set; }
        
        public string? PreviewImage { get; set; }
        public string? File { get; set; }
        public string? FileExtension { get; set; }
        public string? Description { get; set; }
        public bool IsOfficial { get; set; }
        public string CoverImageUrl { get; set; }
        public decimal Price { get; set; }
        public string PriceSymbol { get; set; }
        public string PriceType { get; set; }
        
        public int Generation { get; set; } = -1;
        public List<MetadataDto> TraitPairsDictionary { get; set; }
        
        // seed only
        public CreateTokenInformation CreateTokenInformation { get; set; }
        public string ShowPriceType { get; set; }
        
        public decimal MaxOfferPrice { get; set; }
        
        public DateTime? MaxOfferEndTime { get; set; }
        
        public TokenDto? MaxOfferToken { get; set; }
        
        public InscriptionInfoDto InscriptionInfo { get; set; }
        
        public int Rank { get; set; }
        public string Level { get; set; }
        public string Grade { get; set; }
        public string Star{ get; set; }
        public string Rarity { get; set; }
    }

    public class CreateTokenInformation
    {
        public string Category { get; set; }
        public string TokenSymbol { get; set; }
        public long? Registered { get; set; }
        public long? Expires { get; set; }
    }
}
public enum ShowPriceType
{
    OTHERMINLISTING,
    MYMINLISTING,
    MAXOFFER,
    LATESTDEAL,
    OTHER
}