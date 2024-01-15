using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index;

public class TokenInfoBase:  NFTMarketEntity<string>
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword]
    public string ChainId { get; set; }
    
    public long BlockHeight { get; set; }

    [Keyword] public string Symbol { get; set; }

    /// <summary>
    /// token contract address
    /// </summary>
    [Keyword] public string TokenContractAddress { get; set; }
    
    public int Decimals { get; set; }
    
    public long Supply { get; set; }
    
    public long TotalSupply { get; set; }

    [Keyword] public string TokenName { get; set; }

    [Keyword] public string Owner { get; set; }
    [Keyword] public string Issuer { get; set; }

    public bool IsBurnable { get; set; }

    public int IssueChainId { get; set; }
    
    public long Issued { get; set; }

    public DateTime CreateTime { get; set; }

    public List<ExternalInfoDictionary> ExternalInfoDictionary { get; set; }
}