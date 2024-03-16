using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Trait;

public class QueryCollectionGenerationInfoInput
{
    [Required]
    public string Id { get; set; }

}