using System;
using System.Collections.Generic;
using NFTMarketServer.Basic;
using NFTMarketServer.Entities;
using NFTMarketServer.NFT.Dtos;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTInfos : IndexerCommonResult<IndexerNFTInfos>
{
    public long? TotalRecordCount { get; set; }
    public List<IndexerNFTInfo> IndexerNftInfos { get; set; }
}

public class IndexerNFTInfo : IndexerCommonResult<IndexerNFTInfo>
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public int IssueChainId { get; set; }
    public string Symbol { get; set; }
    public string Issuer { get; set; }
    public string ProxyIssuerAddress { get; set; }
    public string Owner { get; set; }
    
    public string RealOwner { get; set; }
    public long AllOwnerCount { get; set; }
    public long Issued { get; set; }
    public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public int Decimals { get; set; }
    public string WhitelistId { get; set; }

    public string CreatorAddress { get; set; }
    public string ImageUrl { get; set; }

    public string CollectionSymbol { get; set; }
    public string CollectionName { get; set; }
    public string CollectionId { get; set; }

    public bool OtherOwnerListingFlag { get; set; }
    public string ListingId { get; set; }
    public string ListingAddress { get; set; }
    public decimal ListingPrice { get; set; }
    public long ListingQuantity { get; set; }
    public DateTime? ListingEndTime { get; set; }
    public DateTime? LatestListingTime { get; set; }

    public decimal LatestDealPrice { get; set; }
    public DateTime LatestDealTime { get; set; }
    public decimal OfferPrice { get; set; }
    
    public decimal MaxOfferPrice { get; set; }

    public IndexerTokenInfo ListingToken { get; set; }
    public IndexerTokenInfo LatestDealToken { get; set; }

    public string PreviewImage { get; set; }
    public string File { get; set; }
    public string FileExtension { get; set; }
    public string Description { get; set; }
    public bool IsOfficial { get; set; }
    public List<ExternalInfoDictionaryDto> ExternalInfoDictionary { get; set; }
    
    public int Generation { get; set; } = -1;
    public List<ExternalInfoDictionary> TraitPairsDictionary { get; set; }

    
    // seed only
    public string SeedOwnedSymbol { get; set; }
    public string SeedType { get; set; }
    public string TokenType { get; set; }
    public long? RegisterTimeSecond { get; set; }
    public long? SeedExpTimeSecond { get; set; }
    public bool HasListingFlag { get; set; }
    public decimal MinListingPrice { get; set; }
    
    public int Rank { get; set; }
    public string Level { get; set; }
    public string Grade { get; set; }
    public string Star{ get; set; }
    public string Rarity { get; set; }
    
    public void OfListingPrice(Boolean hasListingFlag, decimal minListingPrice, IndexerTokenInfo listingToken)
    {
        ListingPrice = hasListingFlag ? minListingPrice : 0;
        ListingToken = ListingPrice == 0 ? null : listingToken;
    }
    
    public (string Description, decimal Price) GetDescriptionAndPrice(decimal queryMaxOfferPrice)
    {
        if (HasListingFlag)
        {
            return (NFTSymbolBasicConstants.BrifeInfoDescriptionPrice, ListingPrice);
        }
        // HasOfferFlag is not very timely
        if (queryMaxOfferPrice > 0)
        {
            return (NFTSymbolBasicConstants.BrifeInfoDescriptionOffer, queryMaxOfferPrice);
        }
        return (string.Empty, -1);
    }
}