using System;

namespace NFTMarketServer.Message;

public class MessageInfoDto
{
    public string Id { get; set; }
    public string Address { get; set; }
    public int Status { get; set; }
    public BusinessType BusinessType { get; set; }
    public SecondLevelType SecondLevelType { get; set; } 
    public string Title { get; set; }
    public string Body { get; set; }
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
}