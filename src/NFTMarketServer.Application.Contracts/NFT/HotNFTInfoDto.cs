using System;
using System.Collections.Generic;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class HotNFTInfoDto: EntityDto<string>
{
    public string CollectionName { get; set; }
    public string CollectionSymbol { get; set; }
    public string CollectionImage { get; set; }
    public string CollectionId { get; set; }
    public string NFTName { get; set; }
    public string NFTSymbol { get; set; }
    public string PreviewImage { get; set; }
    public string NFTId { get; set; }
    public decimal LatestDealPrice { get; set; }
    public decimal OfferPrice { get; set; }
    public decimal Price { get; set; }
    public string Link { get; set; }
    
    public string ChainId { get; set; }
    
    public int Rank { get; set; }
    public string Level { get; set; }
    public string Grade { get; set; }
    public string Star{ get; set; }
    public string Rarity { get; set; }
    public string Describe { get; set; }
}