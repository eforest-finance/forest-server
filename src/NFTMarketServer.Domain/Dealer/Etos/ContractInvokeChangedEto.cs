using NFTMarketServer.Dealer.Dtos;
using Volo.Abp.EventBus;

namespace NFTMarketServer.Dealer.Etos;

[EventName("ContractInvokeChangedEto")]
public class ContractInvokeChangedEto
{
    public ContractInvokeGrainDto ContractInvokeGrainDto { get; set; }
    
}