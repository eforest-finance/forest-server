namespace NFTMarketServer.NFT;

public class SearchCollectionsFloorPriceDto : PagedAndMaxCountResultRequestDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public decimal FloorPrice { get; set; } = -1;
    public string FloorPriceSymbol { get; set; }
}