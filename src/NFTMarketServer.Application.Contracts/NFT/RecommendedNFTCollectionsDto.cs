using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class RecommendedNFTCollectionsDto : EntityDto<string>
{
    public string Symbol { get; set; }
    
    public string TokenName { get; set; }
    
    public string LogoImage { get; set; }
}