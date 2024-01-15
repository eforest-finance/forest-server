using System;

namespace NFTMarketServer.Order.Dto;

public class PortkeyNotifyReleaseResultParam : NftMerchantBaseDto
{
    public Guid MerchantOrderId { get; set; }
    public string ReleaseTransactionId { get; set; }
    public string ReleaseResult { get; set; }
}