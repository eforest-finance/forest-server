using System;

namespace NFTMarketServer.OwnerShip.Handler;

public class SetIssueAddressEvent
{
    public Guid Id { get; set; }
    public bool Success { get; set; }
    public String TransactionId { get; set; }
}