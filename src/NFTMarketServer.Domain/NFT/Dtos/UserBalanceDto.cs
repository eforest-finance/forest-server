using System;

namespace NFTMarketServer.NFT.Dtos;

public class UserBalanceDto
{
    public string Id { get; set; }
    
    //userAccount Address
     public string Address { get; set; }
    
    public long Amount { get; set; }
    
    public string NFTInfoId { get; set; }

    public string Symbol { get; set; }

    public DateTime ChangeTime { get; set; }
    
    public decimal ListingPrice { get; set; }
    public DateTime? ListingTime { get; set; }
}

