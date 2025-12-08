
using NFTMarketServer.NFT;

namespace NFTMarketServer.Grains.Grain.NFTInfo;
[GenerateSerializer]
public class NftCollectionExtensionGrainDto
{
    [Id(0)]
    public string Id    { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public string NFTSymbol { get; set; }

    [Id(3)]
    public string LogoImage { get; set; }

    [Id(4)]
    public string FeaturedImage { get; set; }

    [Id(5)]
    public string Description { get; set; }

    [Id(6)]
    public string TransactionId { get; set; }
    [Id(7)]
    public string ExternalLink { get; set; }

    [Id(8)]
    public string OldFile { get; set; }

    [Id(9)]
    public long ItemTotal { get; set; }

    [Id(10)]
    public long OwnerTotal { get; set; }

    [Id(11)]
    public decimal FloorPrice { get; set; } = -1;

    [Id(12)]
    public string FloorPriceSymbol { get; set; }

    [Id(13)]
    public string TokenName { get; set; }

    [Id(14)]
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