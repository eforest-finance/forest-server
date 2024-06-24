using System;
using System.Collections.Generic;
using NFTMarketServer.NFT.Index;

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
    
    public string CollectionId { get; set; }
    public string FullAddress { get; set; }
    public int Decimals { get; set; }
    public long BlockHeight { get; set; }
}

public class IndexerUserBalance : IndexerCommonResult<IndexerUserBalance>
{
    public long TotalCount { get; set; }
    public List<UserBalanceDto> IndexerUserBalances { get; set; }
}

