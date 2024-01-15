using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.BId.Index;

public class SymbolBidInfoIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string Symbol { get; set; }
    
    [Keyword] public string AuctionId { get; set; }
    
    [Keyword] public string Bidder { get; set; }

    public long PriceAmount { get; set; }

    [Keyword] public string PriceSymbol { get; set; }

    public long BidTime { get; set; }
    
    public long BlockHeight { get; set; }
    [Keyword] public string TransactionHash { get; set; }
    
}