using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;
using NFTMarketServer.NFT.Etos;

namespace NFTMarketServer.NFT.Index;

public class NFTActivityIndex :  NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string NftInfoId { get; set; }
    
    public int Decimals { get; set; }
    
    public NFTActivityType Type { get; set; }
    
    [Keyword] public string From { get; set; }
    
    [Keyword] public string To { get; set; }
    
    [Text(Index = false)] public string FullFromAddress { get; set; }
    
    [Text(Index = false)] public string FullToAddress { get; set; }
    
    public long Amount { get; set; }
    
    public decimal Price { get; set; }
    
    [Keyword] public string TransactionHash { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    [Keyword] public string CollectionId { get; set; }
    
    [Keyword] public string NFTName { get; set; }
    
    public NFTType NFTType { get; set; }
    
    [Keyword] public string CollectionName { get; set; }

    [Text(Index = false)] public string NFTImage { get; set; }

    public bool ToNFTIssueFlag { get; set; }
    
    public TokenInfoIndex PriceTokenInfo { get; set; }
    
    [Keyword]
    public string ChainId { get; set; }

}