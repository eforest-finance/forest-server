using System;

namespace NFTMarketServer.Order.Handler;

public class CreateSeedResultEvent
{
    public Guid Id { get; set; }
    public bool Success { get; set; }
    public string TransactionId { get; set; }
}