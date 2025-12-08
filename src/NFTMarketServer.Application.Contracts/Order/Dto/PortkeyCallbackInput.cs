using System;

namespace NFTMarketServer.Order.Dto;

public class PortkeyCallbackInput : NftMerchantBaseDto
{
    public Guid MerchantOrderId { get; set; }
    public Guid OrderId { get; set; }
    public string Status { get; set; }
}

public class NftMerchantBaseDto
{
    public string MerchantName { get; set; }
    public string Signature { get; set; }
}