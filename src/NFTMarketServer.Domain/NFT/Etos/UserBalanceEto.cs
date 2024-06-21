using NFTMarketServer.NFT.Dtos;
using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Etos;

[EventName("UserBalanceEto")]
public class UserBalanceEto
{
    public UserBalanceDto UserBalanceDto { get; set; }
}