using System;

namespace NFTMarketServer.Order.Dto;

public class PortkeySearchOrderResultDto : NftMerchantBaseDto
{
    public Guid MerchantOrderId { get; set; }
    public string Status { get; set; }
}