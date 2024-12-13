/*using EventFlow.Aggregates;

namespace NFTMarketServer.EventFlow;

public class OrderAggregate : AggregateRoot<OrderAggregate, OrderId>
{
    private string _productName;
    private int _quantity;

    public OrderAggregate(OrderId id) : base(id) { }

    public void CreateOrder(string productName, int quantity)
    {
        Emit(new OrderCreatedEvent(productName, quantity));
    }

    public void Apply(OrderCreatedEvent aggregateEvent)
    {
        _productName = aggregateEvent.ProductName;
        _quantity = aggregateEvent.Quantity;
    }
}*/