using Orleans;

namespace NFTMarketServer.Dealer.Dtos;
[GenerateSerializer]
public class TransactionStatus
{
    [Id(0)]
    public string TimeStamp { get; set; }
    [Id(1)]
    public string Status { get; set; }
    [Id(2)]
    public string TransactionResult { get; set; }
}