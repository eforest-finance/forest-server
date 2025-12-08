using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;
using NFTMarketServer.Seed.Dto;

namespace NFTMarketServer.BId.Index;


public class SymbolAuctionInfoIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword]  public override string Id { get; set; }

    [Keyword] public string Symbol { get; set; }
    
    [Keyword] public string CollectionSymbol { get; set; }
    
    public TokenPriceDto StartPrice { get; set; }

    public long StartTime { get; set; }

    public long EndTime { get; set; }

    public long MaxEndTime { get; set; }
    
    public long Duration { get; set; }
    
    public int FinishIdentifier { get; set; }
    
    public int MinMarkup { get; set; }

    [Keyword] public string FinishBidder { get; set; }

    public long FinishTime { get; set; }
    
    public TokenPriceDto FinishPrice { get; set; }
        
    [Keyword] public string ReceivingAddress { get; set; }
    
    [Keyword] public string Creator { get; set; }
    
    public long BlockHeight { get; set; }
    
    [Keyword] public string TransactionHash{ get; set; }
    
}