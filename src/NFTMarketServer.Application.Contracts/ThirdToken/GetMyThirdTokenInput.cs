using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.ThirdToken;

public class GetMyThirdTokenInput
{
    [Required] public string Address { get; set; }
    [Required] public string AelfToken { get; set; }
}