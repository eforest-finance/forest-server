using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Basic;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Seed.Dto;
using TokenType = NFTMarketServer.Seed.Dto.TokenType;

namespace NFTMarketServer.Seed.Index;

public class SeedSymbolIndex: TokenInfoBase, IIndexBuild
{
    [Wildcard] public string SeedOwnedSymbol { get; set; }
    
    public long SeedExpTimeSecond { get; set; }

    public DateTime SeedExpTime { get; set; }

    public long RegisterTimeSecond { get; set; }

    public DateTime RegisterTime { get; set; }

    [Keyword] public string IssuerTo { get; set; }
    
    public bool IsDeleteFlag { get; set; }
    
    public TokenType TokenType { get; set; }

    public SeedType SeedType { get; set; }
    
    public decimal Price { get; set; }
    
    [Keyword] public string PriceSymbol { get; set; }
    
    public decimal BeginAuctionPrice { get; set; }
    
    public decimal AuctionPrice { get; set; }
    
    public string AuctionPriceSymbol { get; set; }
    
    public DateTime AuctionDateTime { get; set; }
    
    public bool OtherOwnerListingFlag { get; set; }
    [Keyword] public string ListingId { get; set; }
    [Keyword] public string ListingAddress { get; set; }
    public decimal ListingPrice { get; set; }
    public long ListingQuantity { get; set; }
    public DateTime? ListingEndTime { get; set; }
    public DateTime? LatestListingTime { get; set; }
    
    public decimal OfferPrice { get; set; }
    
    public long OfferQuantity { get; set; }
    
    public DateTime OfferExpireTime { get; set; }
    
    public DateTime? LatestOfferTime { get; set; }
    
    public TokenInfoIndex OfferToken { get; set; }
    public TokenInfoIndex ListingToken { get; set; }
    public TokenInfoIndex LatestDealToken { get; set; }

    public SeedStatus? SeedStatus { get; set; }
    
    public bool HasOfferFlag { get; set; }
    public bool HasListingFlag { get; set; }
    public decimal MinListingPrice { get; set; }
    
    public DateTime? MinListingExpireTime { get; set; }

    [Keyword] public string MinListingId { get; set; }
    
    public bool HasAuctionFlag { get; set; }
    public decimal MaxAuctionPrice { get; set; }
    
    public decimal MaxOfferPrice { get; set; }

    public DateTime? MaxOfferExpireTime { get; set; }
    
    [Keyword] public string MaxOfferId { get; set; }
    
    [Keyword] public string SeedImage { get; set; }
    
    [Text(Index = false)] public string RealOwner { get; set; }
    public long AllOwnerCount { get; set; }

    public decimal LatestDealPrice { get; set; } = -1;
    [Keyword] public string LatestDealId { get; set; }
    
    public DateTime? LatestDealTime { get; set; }
    
    public (string Description, decimal Price) GetDescriptionAndPrice(decimal queryMaxOfferPrice)
    {
        if (HasAuctionFlag)
        {
            return (NFTSymbolBasicConstants.BrifeInfoDescriptionTopBid, MaxAuctionPrice);
        }

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