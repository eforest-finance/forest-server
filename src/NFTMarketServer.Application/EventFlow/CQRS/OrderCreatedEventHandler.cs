using System.Threading.Tasks;
using Nest;
using NFTMarketServer.EventFlow.CQRS;
using Volo.Abp.EventBus;

public class OrderCreatedEventHandler : ILocalEventHandler<OrderCreatedEvent>
{
    private readonly IElasticClient _elasticClient;
    
    public OrderCreatedEventHandler(IElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    public async Task HandleEventAsync(OrderCreatedEvent eventData)
    {
        var orderDocument = new
        {
            Id = eventData.OrderId,
            ProductName = eventData.ProductName,
            Quantity = eventData.Quantity
        };

        await _elasticClient.IndexDocumentAsync(orderDocument);
    }
}