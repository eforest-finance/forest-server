using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class SearchNFTCollectionsDto : EntityDto<string>
{
    
    public string ChainId { get; set; }
    
    public string Symbol { get; set; }
    
    public string TokenName { get; set; }
    
    public string LogoImage { get; set; }

    public long ItemTotal { get; set; }
    
    public long OwnerTotal { get; set; }
    
    public decimal FloorPrice { get; set; } = -1;
    
    public string FloorPriceSymbol { get; set; }
}