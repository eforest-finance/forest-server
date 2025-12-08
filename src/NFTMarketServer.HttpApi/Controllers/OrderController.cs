using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Order;
using NFTMarketServer.Order.Dto;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Order")]
[Route("api/app/order")]
public class OrderController : AbpController
{
    private readonly IOrderAppService _orderAppService;

    public OrderController(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    [HttpPost]
    [Route("create")]
    [Authorize]
    public virtual Task<CreateOrderResultDto> CreateOrder(CreateOrderInput input)
    {
        return _orderAppService.CreateOrderAsync(input);
    }
    
    [HttpPost]
    [Route("search")]
    [Authorize]
    public virtual Task<NFTOrderDto> SearchOrder(SearchOrderInput input)
    {
        return _orderAppService.SearchOrderAsync(input);
    }
}