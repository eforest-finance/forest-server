using System;
using Volo.Abp.Application.Dtos;
using System.Collections.Generic;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT;

public class NFTDropDetailDto : EntityDto<string>
{
    public string DropId { get; set; }
    public string DropName { get; set; }
    public string LogoUrl { get; set; }
    public string BannerUrl { get; set; }
    public decimal ClaimPrice { get; set; }
    public decimal ClaimPriceUsd { get; set; }
    public string Introduction { get; set; }
    
    public string CollectionId { get; set; }
    public string CollectionLogo { get; set; }
    public string CollectionName { get; set; }
    
    public long TotalAmount { get; set; }
    public long ClaimAmount { get; set; }
    public long AddressClaimLimit { get; set; }
    public long AddressClaimAmount { get; set; }
    public NFTDropState State { get; set; }
    
    public bool Burn { get; set; }
    public long StartTime { get; set; }
    public long ExpireTime { get; set; }
    public List<NFTMarketServer.NFT.Index.SocialMedia> SocialMedia { get; set; }
}

