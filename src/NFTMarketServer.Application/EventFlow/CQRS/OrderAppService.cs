using System;
using System.Threading.Tasks;
using NFTMarketServer.EventFlow.CQRS;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;

public class OrderAppService : ApplicationService
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly ILocalEventBus _localEventBus;

    public OrderAppService(IRepository<Order, Guid> orderRepository, ILocalEventBus localEventBus)
    {
        _orderRepository = orderRepository;
        _localEventBus = localEventBus;
    }

    public async Task CreateOrderAsync(CreateOrderCommand command)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ProductName = command.ProductName,
            Quantity = command.Quantity
        };

        await _orderRepository.InsertAsync(order);

        await _localEventBus.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            ProductName = order.ProductName,
            Quantity = order.Quantity
        });
    }
}