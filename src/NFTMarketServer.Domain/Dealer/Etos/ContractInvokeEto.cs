using NFTMarketServer.Dealer.Dtos;
using Volo.Abp.EventBus;

namespace NFTMarketServer.Dealer.Etos;

[EventName("ContractInvokeEto")]
public class ContractInvokeEto
{
    public ContractParamDto ContractParamDto { get; set; }
    
}