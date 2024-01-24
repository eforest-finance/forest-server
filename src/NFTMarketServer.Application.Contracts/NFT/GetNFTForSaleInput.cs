using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.NFT;

public class GetNFTForSaleInput
{
    [Required] public string Id { get; set; }
    
    public string ExcludedAddress { get; set; }
}