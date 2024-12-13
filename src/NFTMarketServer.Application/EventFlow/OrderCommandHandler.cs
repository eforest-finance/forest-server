/*using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;

namespace NFTMarketServer.EventFlow;

public class OrderCommandHandler : CommandHandler<OrderAggregate, OrderId, CreateOrderCommand>
{
    public override Task ExecuteAsync(OrderAggregate aggregate, CreateOrderCommand command, CancellationToken cancellationToken)
    {
        aggregate.CreateOrder(command.ProductName, command.Quantity);
        return Task.CompletedTask;
    }
}*/