using System;
using NFTMarketServer.NFT;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class NFTDropIndexDto : EntityDto<string>
{
    public string DropId { get; set; }
    public string DropName { get; set; }
    public string LogoUrl { get; set; }
    public decimal MintPrice { get; set; }
    public decimal MintPriceUsd { get; set; }
    public string Introduction { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime ExpireTime { get; set; }
}

public class RecommendedNFTDropIndexDto : EntityDto<string>
{
    public string DropId { get; set; }
    public string DropName { get; set; }
    public string BannerUrl { get; set; }
    public decimal MintPrice { get; set; }
    public decimal MintPriceUsd { get; set; }
    public string Introduction { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime ExpireTime { get; set; }
}