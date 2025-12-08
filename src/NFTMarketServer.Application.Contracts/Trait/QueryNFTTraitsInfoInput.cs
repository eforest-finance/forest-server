using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Trait;

public class QueryNFTTraitsInfoInput
{
    [Required]
    public string Id { get; set; }

}