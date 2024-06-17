using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace NFTMarketServer.Message;

public class MessageInfoIndex : IIndexBuild
{
    [Keyword] public string Id { get; set; }
    [Keyword] public string Address { get; set; }
    public int status { get; set; }
    public BusinessType BusinessType { get; set; }
    public SecondLevelType SecondLevelType { get; set; }
    [Keyword] public string Title { get; set; }
    [Text(Index = false)] public string Body { get; set; }
    public string Image { get; set; }
    public int Decimal { get; set; }
    public string PriceType { get; set; }
    public string SinglePrice { get; set; }
    public string TotalPrice { get; set; }
    public string BusinessId{ get; set; }
    public string Amount { get; set; }
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
//消息二级分类
public enum SecondLevelType
{
    SOLD,
    RECEIVEOFFER,
    ACCEPTOFFER
}

public abstract class ExternalInfoDictionary
{
    [Keyword] public string Key { get; set; }
    [Keyword] public string Value { get; set; }
}