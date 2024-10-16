using System;
using Nest;
using Orleans;

namespace NFTMarketServer.Order;
[GenerateSerializer]
public class NFTOrder : NFTOrderBase
{
    [Id(0)]
    public Guid UserId { get; set; }

    // portkey orderId
    [Id(1)]
    public Guid ThirdPartOrderId { get; set; }
    // user address
    [Keyword][Id(2)] public string Address { get; set; }
    // buyer network, ELF
    [Keyword][Id(3)] public string Network { get; set; }
    [Keyword][Id(4)] public string ChainId {get;set;}
    [Id(5)]
    public long CreateTime { get; set; }
    [Id(6)]
    public long LastModifyTime { get; set; }
    [Keyword][Id(7)] public string NftReleaseTransactionId { get; set; }
    [Id(8)]
    public OrderStatus OrderStatus { get; set; }
}