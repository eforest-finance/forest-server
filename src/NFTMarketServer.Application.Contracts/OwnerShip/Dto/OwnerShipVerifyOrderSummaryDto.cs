using System;
using NFTMarketServer.Symbol;

namespace NFTMarketServer.OwnerShip.Dto;

public class OwnerShipVerifyOrderSummaryDto
{
    public string Symbol { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public Guid Id { get; set; }
    public string From { get; set; }
    public long SubmitTime { get; set; }
}