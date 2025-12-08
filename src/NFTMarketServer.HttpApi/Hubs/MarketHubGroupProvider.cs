using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Hubs;


public interface IMarketHubGroupProvider
{
    string GetMarketBidInfoGroupName(string symbol);

    string GetMarketAuctionInfoGroupName(string symbol);

    List<string> GetAllBidInfoGroup();

    List<string> GetAllAuctionInfoGroup();

    string QueryNameForReceiveListingChangeSignal(string symbol);
    string QueryMethodNameForReceiveListingChangeSignal();
    
    string QueryNameForReceiveMessageChangeSignal(string address);
    string QueryMethodNameForReceiveMessageChangeSignal();

    string GetNtfOfferChangeGroupName(string nftId);
    List<string> GetAllNftOfferChangeGroup();
}

public class MarketHubGroupProvider : IMarketHubGroupProvider, ISingletonDependency
{
    private const string RECEIVE_LISTING_CHANGE_SIGNAL_NAME = "ReceiveListingChangeSignal";
    private const string RECEIVE_MESSAGE_CHANGE_SIGNAL_NAME = "ReceiveMessageChangeSignal";
    
    private readonly ConcurrentDictionary<string, string> _bidInfoGroups = new();
    private readonly ConcurrentDictionary<string, string> _auctionInfoGroups = new();
    private readonly ConcurrentDictionary<string, string> _nftOfferChangeGroups = new();

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

    public string QueryNameForReceiveListingChangeSignal(string symbol)
    {
        var groupName = RECEIVE_LISTING_CHANGE_SIGNAL_NAME + "-" + symbol;
        _bidInfoGroups.TryAdd(groupName, string.Empty);
        return groupName;
    }

    public string QueryMethodNameForReceiveMessageChangeSignal()
    {
        return RECEIVE_MESSAGE_CHANGE_SIGNAL_NAME;
    }
    
    public string QueryNameForReceiveMessageChangeSignal(string address)
    {
        var groupName = RECEIVE_MESSAGE_CHANGE_SIGNAL_NAME + "-" + address;
        _bidInfoGroups.TryAdd(groupName, string.Empty);
        return groupName;
    }

    public string QueryMethodNameForReceiveListingChangeSignal()
    {
        return RECEIVE_LISTING_CHANGE_SIGNAL_NAME;
    }

    public string GetNtfOfferChangeGroupName(string nftId)
    {
        var groupName = $"NFTOfferChange-{nftId}";
        _nftOfferChangeGroups.TryAdd(groupName, string.Empty);
        return groupName;
    }
    
    public List<string> GetAllNftOfferChangeGroup()
    {
        return _nftOfferChangeGroups.Keys.ToList();
    }
}