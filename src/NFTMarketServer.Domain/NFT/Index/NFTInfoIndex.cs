using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Basic;

namespace NFTMarketServer.NFT.Index;

public class NFTInfoIndex : TokenInfoBase, IIndexBuild
{
    [Keyword] public HashSet<string> IssueManagerSet { get; set; }

    [Keyword] public string RandomIssueManager { get; set; }
    
    [Keyword] public string CreatorAddress { get; set; }
    [Keyword] public string ImageUrl { get; set; }
    
    [Keyword] public string CollectionSymbol { get; set; }
    [Keyword] public string CollectionName { get; set; }
    [Keyword] public string CollectionId { get; set; }
    
    public bool OtherOwnerListingFlag { get; set; }
    [Keyword] public string ListingId { get; set; }
    [Keyword] public string ListingAddress { get; set; }
    public decimal ListingPrice { get; set; }
    public long ListingQuantity { get; set; }
    public DateTime? ListingEndTime { get; set; }
    public DateTime? LatestListingTime { get; set; }
    public DateTime? LatestOfferTime { get; set; }
    public decimal LatestDealPrice { get; set; }
    public DateTime LatestDealTime { get; set; }
    public decimal OfferPrice { get; set; }
    public long OfferQuantity { get; set; }
    public DateTime OfferExpireTime { get; set; }
    public TokenInfoIndex OfferToken { get; set; }
    public TokenInfoIndex ListingToken { get; set; }
    public TokenInfoIndex LatestDealToken { get; set; }
    public TokenInfoIndex WhitelistPriceToken { get; set; }

    [Keyword] public string PreviewImage { get; set; }
    [Keyword] public string File { get; set; }
    [Keyword] public string FileExtension { get; set; }
    [Keyword] public string Description { get; set; }
    public bool IsOfficial { get; set; }
    
    public bool HasListingFlag { get; set; }
    public decimal MinListingPrice { get; set; }
    
    public DateTime? MinListingExpireTime { get; set; }

    [Keyword] public string MinListingId { get; set; }
    
    public bool HasOfferFlag { get; set; }
    
    public decimal MaxOfferPrice { get; set; }

    public DateTime? MaxOfferExpireTime { get; set; }
    
    [Keyword] public string MaxOfferId { get; set; }
    
    
    public (string Description, decimal Price) GetDescriptionAndPrice(decimal queryMaxOfferPrice)
    {
        if (HasListingFlag)
        {
            return (NFTSymbolBasicConstants.BrifeInfoDescriptionPrice, MinListingPrice);
        }
        // HasOfferFlag is not very timely
        if (queryMaxOfferPrice > 0)
        {
            return (NFTSymbolBasicConstants.BrifeInfoDescriptionOffer, queryMaxOfferPrice);
        }
        return (string.Empty, -1);
    }
}