using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.Message;

public class MessageInfoIndex :  NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Address { get; set; }
    public int Status { get; set; }
    public BusinessType BusinessType { get; set; }
    public SecondLevelType SecondLevelType { get; set; }
    [Keyword] public string Title { get; set; }
    [Text(Index = false)] public string Body { get; set; }
    public string Image { get; set; }
    public int Decimal { get; set; }
    public string PriceType { get; set; }
    public decimal SinglePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string BusinessId{ get; set; }
    public long Amount { get; set; }
    public string WebLink { get; set; } 
    public string AppLink { get; set; } 
    public DateTime Ctime { get; set; }
    public DateTime Utime { get; set; } 
    [Nested]
    public List<ExternalInfoDictionary> ExternalInfoDictionary { get; set; }
}

public enum BusinessType
{
    TRANSACTION,
    MARKETING,
    NOTIFICATIONS
}

public enum SecondLevelType
{
    SELL,
    BUY,
    RECEIVEOFFER
}

public abstract class ExternalInfoDictionary
{
    [Keyword] public string Key { get; set; }
    [Keyword] public string Value { get; set; }
}