using System;
using Azure.Messaging.EventHubs;
using Volo.Abp.EventBus;

namespace NFTMarketServer.EventFlow.CQRS;

public class OrderCreatedEvent : EventData
{
    public Guid OrderId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
}