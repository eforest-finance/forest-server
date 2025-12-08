using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.SymbolMarketToken;

public class GetSymbolMarketTokenExistInput
{
    [Required] public string IssueChainId { get; set; }
    [Required] public string TokenSymbol { get; set; }
}