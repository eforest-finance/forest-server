using System;

namespace NFTMarketServer.Order.Dto;

public class NFTOrderDto
{
    public Guid OrderId { get; set; }
    public Guid MerchantOrderId { get; set; }
    public string NftSymbol { get; set; }
    public string MerchantName { get; set; }
    public OrderStatus OrderStatus { get; set; }
}