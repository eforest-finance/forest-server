using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Trait;

public class QueryCollectionRarityInfoInput
{
    [Required]
    public string Id { get; set; }

}