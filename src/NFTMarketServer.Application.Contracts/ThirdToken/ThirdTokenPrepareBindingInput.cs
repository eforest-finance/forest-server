using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.ThirdToken;

public class ThirdTokenPrepareBindingInput
{
    [Required] public string Address { get; set; }
    [Required] public string AelfToken { get; set; }
    [Required] public string AelfChain { get; set; }
    [Required] public ThirdTokenDto ThirdTokens { get; set; }
    [Required] public string Signature { get; set; }
}

public class ThirdTokenDto
{
    [Required] public string TokenName { get; set; }
    [Required] public string Symbol { get; set; }
    [Required] public string TokenImage { get; set; }
    [Required] public long TotalSupply { get; set; }
    [Required] public string Owner { get; set; }
    [Required] public string ThirdChain { get; set; }
    [Required] public string ContractAddress { get; set; }
}