using Google.Protobuf.Collections;

namespace NFTMarketServer.Inscription.Dto;

public class ValidateTokenInfoExistsParam
{
    public string TokenName { get; set; }
    public string TotalSupply { get; set; }
    public string Issuer { get; set; }
    public string IssueChainId { get; set; }
    public string Owner { get; set; }
    public string Symbol { get; set; }

    public MapField<string, string> ExternalInfo { get; set; }
}