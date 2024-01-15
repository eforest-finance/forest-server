using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using Volo.Abp.AspNetCore.SignalR;

namespace MarketServer.Hubs;

public class MarketHub  : AbpHub
{
    private readonly IBidAppService _bidAppService;
    private readonly IMarketHubGroupProvider _marketHubGroupProvider;

    public MarketHub(IBidAppService bidAppService, IMarketHubGroupProvider marketHubGroupProvider)
    {
        _bidAppService = bidAppService;
        _marketHubGroupProvider = marketHubGroupProvider;
    }

    public async Task RequestSymbolBidInfo(string seedSymbol, int maxResultCount = 1000)
    {
        if (string.IsNullOrEmpty(seedSymbol))
        {
            return;
        }
    
        var list = await _bidAppService.GetSymbolBidInfoListAsync(new GetSymbolBidInfoListRequestDto
        {
            SeedSymbol = seedSymbol,
            MaxResultCount = maxResultCount
        });
    
        await Groups.AddToGroupAsync(Context.ConnectionId,
            _marketHubGroupProvider.GetMarketBidInfoGroupName(seedSymbol));
        await Clients.Caller.SendAsync("ReceiveSymbolBidInfos", list);
    }
    
    public async Task UnsubscribeSymbolBidInfo(string seedSymbol)
    {
        if (string.IsNullOrEmpty(seedSymbol))
        {
            return;
        }
    
        await TryRemoveFromGroupAsync(Context.ConnectionId,
            _marketHubGroupProvider.GetMarketBidInfoGroupName(seedSymbol));
    }
    
    public async Task RequestSymbolAuctionInfo(string seedSymbol)
    {
        if (string.IsNullOrEmpty(seedSymbol))
        {
            return;
        }
    
        var auctionInfoDto = await _bidAppService.GetSymbolAuctionInfoAsync(seedSymbol);
    
        await Groups.AddToGroupAsync(Context.ConnectionId,
            _marketHubGroupProvider.GetMarketAuctionInfoGroupName(seedSymbol));
    
        await Clients.Caller.SendAsync("ReceiveSymbolAuctionInfo", auctionInfoDto != null ? auctionInfoDto : null);
    }
    
    public async Task UnsubscribeSymbolAuctionInfo(string seedSymbol)
    {
        if (string.IsNullOrEmpty(seedSymbol))
        {
            return;
        }
    
        await TryRemoveFromGroupAsync(Context.ConnectionId,
            _marketHubGroupProvider.GetMarketAuctionInfoGroupName(seedSymbol));
    }
    
    private async Task<bool> TryRemoveFromGroupAsync(string connectionId, string groupName)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(connectionId, groupName);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var bidGroups = _marketHubGroupProvider.GetAllBidInfoGroup();
        foreach (var group in bidGroups)
        {
            await TryRemoveFromGroupAsync(Context.ConnectionId, group);
        }
    
        var auctionGroups = _marketHubGroupProvider.GetAllAuctionInfoGroup();
        foreach (var group in auctionGroups)
        {
            await TryRemoveFromGroupAsync(Context.ConnectionId, group);
        }
    
        await base.OnDisconnectedAsync(exception);
    }
}