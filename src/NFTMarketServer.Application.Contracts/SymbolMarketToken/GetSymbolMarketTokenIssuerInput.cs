using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.SymbolMarketToken;

public class GetSymbolMarketTokenIssuerInput
{
    [Required] public int IssueChainId { get; set; }
    [Required] public string TokenSymbol { get; set; }
}