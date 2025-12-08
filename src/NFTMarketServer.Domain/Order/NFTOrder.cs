using System;
using Nest;

namespace NFTMarketServer.Order;

public class NFTOrder : NFTOrderBase
{
    public Guid UserId { get; set; }
    // portkey orderId
    public Guid ThirdPartOrderId { get; set; }
    // user address
    [Keyword] public string Address { get; set; }
    // buyer network, ELF
    [Keyword] public string Network { get; set; }
    [Keyword] public string ChainId {get;set;}
    public long CreateTime { get; set; }
    public long LastModifyTime { get; set; }
    [Keyword] public string NftReleaseTransactionId { get; set; }
    public OrderStatus OrderStatus { get; set; }
}