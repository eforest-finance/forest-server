using System;

namespace NFTMarketServer.Order.Dto;

public class PortkeyCreateOrderResultDto : NftMerchantBaseDto
{
    public Guid OrderId { get; set; }
}