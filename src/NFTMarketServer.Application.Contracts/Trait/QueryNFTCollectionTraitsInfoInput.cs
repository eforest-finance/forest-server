using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Trait;

public class QueryNFTCollectionTraitsInfoInput : PagedAndMaxCountResultRequestDto
{
    [Required] public string Id { get; set; }
}