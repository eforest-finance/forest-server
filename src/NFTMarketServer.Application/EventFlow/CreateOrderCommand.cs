/*using EventFlow.Commands;

namespace NFTMarketServer.EventFlow;

public class CreateOrderCommand : Command<OrderAggregate, OrderId>
{
    public string ProductName { get; }
    public int Quantity { get; }

    public CreateOrderCommand(OrderId aggregateId, string productName, int quantity)
        : base(aggregateId)
    {
        ProductName = productName;
        Quantity = quantity;
    }
}*/