using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace MarketServer.Hubs;


public interface IMarketHubGroupProvider
{
    string GetMarketBidInfoGroupName(string symbol);

    string GetMarketAuctionInfoGroupName(string symbol);

    List<string> GetAllBidInfoGroup();

    List<string> GetAllAuctionInfoGroup();
}

public class MarketHubGroupProvider : IMarketHubGroupProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, string> _bidInfoGroups = new();
    private readonly ConcurrentDictionary<string, string> _auctionInfoGroups = new();

    public string GetMarketBidInfoGroupName(string symbol)
    {
        var groupName = $"MarketBidInfo-{symbol}";
        _bidInfoGroups.TryAdd(groupName, string.Empty);
        return groupName;
    }

    public string GetMarketAuctionInfoGroupName(string symbol)
    {
        var groupName = $"MarketAuctionInfo-{symbol}";
        _auctionInfoGroups.TryAdd(groupName, string.Empty);
        return groupName;
    }

    public List<string> GetAllBidInfoGroup()
    {
        return _bidInfoGroups.Keys.ToList();
    }

    public List<string> GetAllAuctionInfoGroup()
    {
        return _auctionInfoGroups.Keys.ToList();
    }
}