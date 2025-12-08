using System;

namespace NFTMarketServer.Order.Dto;

public class PortkeySearchOrderParam : NftMerchantBaseDto
{
    public Guid MerchantOrderId { get; set; }
    public Guid OrderId { get; set; }
}