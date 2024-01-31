using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT;

public class GetNFTDropQuotaInput
{
    [Required] public string DropId { get; set; }
    
    [Required] public string Address { get; set; }
}