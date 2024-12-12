using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.ThirdToken;

public class ThirdTokenBindingInput
{
    [Required] public string BindingId { get; set; }
    [Required] public string ThirdTokenId { get; set; }
    [Required] public string Signature { get; set; }
}