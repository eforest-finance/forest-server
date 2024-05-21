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
    public string Link { get; set; }
    
    public string ChainId { get; set; }
}