using System;

namespace NFTMarketServer.Order.Dto;

public class PortkeyCreateOrderParam : NftMerchantBaseDto
{
    public string NftSymbol { get; set; }
    public Guid MerchantOrderId { get; set; }
    public string WebhookUrl { get; set; }
    public string PaymentSymbol { get; set; }
    public string PaymentAmount { get; set; }
    public string CaHash { get; set; }
    public string TransDirect { get; set; } = OrderConstants.NftBuy;
}