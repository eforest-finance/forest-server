using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.NFT;

public class GetNFTDropDetailInput
{
    [Required] public string DropId { get; set; }
    
    public string Address { get; set; }
}