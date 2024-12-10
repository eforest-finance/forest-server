using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.SymbolMarketToken;

public class SymbolMarketTokenDto : EntityDto<string>
{
    public string TokenName { get; set; }
    public string Symbol { get; set; }
    public string TokenImage { get; set; }
    public string Issuer { get; set; }
    public string Owner { get; set; }
    public int Decimals { get; set; }
    public long TotalSupply { get; set; }
    public long CurrentSupply { get; set; }
    public string IssueChain { get; set; }
    
    public int IssueChainId { get; set; }
    public string OriginIssueChain { get; set; }
    public string TokenAction { get; set; }
}