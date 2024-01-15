
using NFTMarketServer.NFT;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class NftCollectionExtensionGrainDto
{
    public string Id    { get; set; }
    public string ChainId { get; set; }
    public string NFTSymbol { get; set; }

    public string LogoImage { get; set; }
        
    public string FeaturedImage { get; set; }
        
    public string Description { get; set; }

    public string TransactionId { get; set; }
    public string ExternalLink { get; set; } 

    public string OldFile { get; set; }
    
    public long ItemTotal { get; set; }
    
    public long OwnerTotal { get; set; }
    
    public decimal FloorPrice { get; set; } = -1;
    
    public string FloorPriceSymbol { get; set; }
    
    public string TokenName { get; set; }
    
    public DateTime CreateTime { get; set; }

    public void OfCollectionExtensionDto(NFTCollectionExtensionDto dto)
    {
        if (dto.FloorPrice != null)
        {
            FloorPrice = dto.FloorPrice.Value;
        }

        if (dto.ItemTotal != null)
        {
            ItemTotal = dto.ItemTotal.Value;
        }

        if (dto.OwnerTotal != null)
        {
            OwnerTotal = dto.OwnerTotal.Value;
        }
    }
    
}