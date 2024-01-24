using System;
using System.Collections.Generic;
using NFTMarketServer.NFT.Dtos;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTOwners : IndexerCommonResult<IndexerNFTOwners>
{
    public long TotalCount { get; set; }
    public List<IndexerNFTUserBalance> IndexerNftUserBalances { get; set; }
}

public class IndexerNFTUserBalance : IndexerCommonResult<IndexerNFTUserBalance>
{
    public string Id { get; set; }
    
    public string Address { get; set; }
    
    public long Amount { get; set; }
}