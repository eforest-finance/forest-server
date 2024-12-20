using NFTMarketServer.ThirdToken.Index;
using Volo.Abp.EventBus;

namespace NFTMarketServer.ThirdToken.Etos;

[EventName("TokenRelationEto")]
public class TokenRelationEto
{
    public string Id { get; set; }
    public string Address { get; set; }
    public string AelfChain { get; set; }
    public string AelfToken { get; set; }
    public string ThirdChain { get; set; }
    public string ThirdToken { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public RelationStatus RelationStatus { get; set; }
    public string ThirdTokenSymbol { get; set; }
}