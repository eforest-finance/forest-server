using System;

namespace NFTMarketServer.Order.Dto;

public class CreateOrderResultDto
{
    public Guid OrderId { get; set; }
    public Guid ThirdPartOrderId { get; set; }
}