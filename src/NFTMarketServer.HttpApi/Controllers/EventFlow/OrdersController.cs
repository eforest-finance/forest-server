/*using System.Threading.Tasks;
using EventFlow;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.EventFlow;

namespace NFTMarketServer.Controllers.EventFlow;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ICommandBus _commandBus;

    public OrdersController(ICommandBus commandBus)
    {
        _commandBus = commandBus;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var orderId = OrderId.New;
        var command = new CreateOrderCommand(orderId, request.ProductName, request.Quantity);
        await _commandBus.PublishAsync(command, HttpContext.RequestAborted);
        return Ok(new { OrderId = orderId });
    }
    public class CreateOrderRequest
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }
}*/