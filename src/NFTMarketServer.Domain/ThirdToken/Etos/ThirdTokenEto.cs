using NFTMarketServer.ThirdToken.Index;
using Volo.Abp.EventBus;

namespace NFTMarketServer.ThirdToken.Etos;

[EventName("ThirdTokenEto")]
public class ThirdTokenEto
{
    public string Id { get; set; }
    public string Symbol { get; set; }
    public string TokenName { get; set; }
    public string Chain { get; set; }
    public long TotalSupply { get; set; }
    public int Decimals { get; set; }
    public string Owner { get; set; }
    public long CreateTime { get; set; }
    public string TokenImage { get; set; }
    public string ContractAddress { get; set; }
    public ThirdTokenStatus ThirdTokenStatus { get; set; }
}