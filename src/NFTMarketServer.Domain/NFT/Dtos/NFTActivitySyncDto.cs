using System;

namespace NFTMarketServer.NFT.Dtos;

public class NFTActivitySyncDto
{
    public string Id { get; set; }
    public string NFTInfoId { get; set; }
    public NFTActivityType Type { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public long Amount { get; set; }
    public TokenInfoDto PriceTokenInfo { get; set; }
    public decimal Price { get; set; }
    public string TransactionHash { get; set; }
    public DateTime Timestamp { get; set; }
    public long BlockHeight { get; set;}
}