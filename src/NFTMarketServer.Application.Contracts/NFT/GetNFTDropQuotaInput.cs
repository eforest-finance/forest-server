using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.NFT;

public class GetNFTDropQuotaInput
{
    [Required] public string DropId { get; set; }
    
    [Required] public string Address { get; set; }
}