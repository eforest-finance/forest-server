using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class TrendingCollectionsDto : EntityDto<string>
{
    
    public string ChainId { get; set; }
    
    public string Symbol { get; set; }
    
    public string TokenName { get; set; }
    
    public string LogoImage { get; set; }
    public string PreviewImage { get; set; }

    public long ItemTotal { get; set; }
    
    public long OwnerTotal { get; set; }
    
    public decimal FloorPrice { get; set; } = -1;
    
    public string FloorPriceSymbol { get; set; }

    public decimal VolumeTotal { get; set; } = 0;
    public decimal VolumeTotalChange { get; set; } = 0;
    public decimal FloorChange { get; set; } = 0;

}