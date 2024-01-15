using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Activity;

public class SymbolMarketActivityDto : EntityDto<string>
{
    public string Symbol { get; set; }
    public long Timestamp { get; set; }
    public SymbolMarketActivityType Type { get; set; }
    public decimal Price { get; set; }
    public string PriceSymbol { get; set; }
    public decimal TransactionFee { get; set; }
    public string TransactionFeeSymbol { get; set; }
    public string TransactionId { get; set; }
}