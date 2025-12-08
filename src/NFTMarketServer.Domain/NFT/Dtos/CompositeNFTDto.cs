using NFTMarketServer.NFT.Etos;

namespace NFTMarketServer.NFT.Dtos;

public class CompositeNFTDto
{
    public string NFTInfoId { get; set; }
    public string NFTName { get; set; }
    public string CollectionId { get; set; }
    public string CollectionName { get; set; }
    public int Decimals { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string CollectionSymbol { get; set; }
    public string PreviewImage { get; set; }
    public NFTType NFTType { get; set; }
}