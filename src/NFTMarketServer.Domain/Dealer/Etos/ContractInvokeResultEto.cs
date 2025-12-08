using AElf.Client.Dto;
using Volo.Abp.EventBus;

namespace NFTMarketServer.Dealer.Etos;

[EventName("ContractInvokeResultEto")]
public class ContractInvokeResultEto
{
    public string BizType { get; set; }
    public string BizId { get; set; }
    public TransactionResultDto TransactionResult { get; set; }
    public string RawTransaction { get; set; }
}