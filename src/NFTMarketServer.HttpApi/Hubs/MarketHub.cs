using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using ImageMagick;
using Microsoft.AspNetCore.SignalR;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.HandleException;
using NFTMarketServer.Helper;
using NFTMarketServer.NFT;
using Volo.Abp.AspNetCore.SignalR;

namespace NFTMarketServer.Hubs;

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
    
    public async Task RequestListingChangeSignal(string nftId)
    {
        if (string.IsNullOrEmpty(nftId) || !SymbolHelper.MatchCommonSymbolPattern(nftId))
        {
            return;
        }
        
        var seedSymbol = nftId.Substring(nftId.IndexOf(SymbolHelper.GetHyphen())+1);
        await Groups.AddToGroupAsync(Context.ConnectionId,
            _marketHubGroupProvider.QueryNameForReceiveListingChangeSignal(seedSymbol));
        
        var signal = new ChangeSignalBaseDto
        {
            HasChanged = false
        };
        await Clients.Caller.SendAsync(_marketHubGroupProvider.QueryMethodNameForReceiveListingChangeSignal(), signal);
    }
    
    public async Task UnsubscribeListingChangeSignal(string seedSymbol)
    {
        if (string.IsNullOrEmpty(seedSymbol))
        {
            return;
        }
        await TryRemoveFromGroupAsync(Context.ConnectionId,
            _marketHubGroupProvider.QueryNameForReceiveListingChangeSignal(seedSymbol));
    }
    
    public async Task RequestMessageChangeSignal(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return;
        }

        address = FullAddressHelper.ToShortAddress(address);
        
        await Groups.AddToGroupAsync(Context.ConnectionId,
            _marketHubGroupProvider.QueryNameForReceiveMessageChangeSignal(address));
        
        var signal = new ChangeSignalBaseDto
        {
            HasChanged = false
        };
        await Clients.Caller.SendAsync(_marketHubGroupProvider.QueryMethodNameForReceiveMessageChangeSignal(), signal);
    }

    public async Task UnsubscribeMessageChangeSignal(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return;
        }

        address = FullAddressHelper.ToShortAddress(address);
        await TryRemoveFromGroupAsync(Context.ConnectionId,
            _marketHubGroupProvider.QueryNameForReceiveMessageChangeSignal(address));
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
    
    public async Task RequestNftOfferChange(string nftId)
    {
        if (string.IsNullOrEmpty(nftId))
        {
            return;
        }

        var signal = new NFTOfferChangeSignalDto
        {
            hasChanged = false
        };
        
        await Groups.AddToGroupAsync(Context.ConnectionId, _marketHubGroupProvider.GetNtfOfferChangeGroupName(nftId));
        await Clients.Caller.SendAsync("ReceiveOfferChangeSignal", signal);
    }
    
    public async Task UnsubscribeNftOfferChange(string nftId)
    {
        if (string.IsNullOrEmpty(nftId))
        {
            return;
        }
    
        await TryRemoveFromGroupAsync(Context.ConnectionId, _marketHubGroupProvider.GetNtfOfferChangeGroupName(nftId));
    }
    [ExceptionHandler(typeof(Exception),
        Message = "MarketHub.TryRemoveFromGroupAsync", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        ReturnDefault = ReturnDefault.Default,
        LogTargets = new []{"connectionId", "groupName" }
    )]
    public virtual async  Task<bool> TryRemoveFromGroupAsync(string connectionId, string groupName)
    {
        await Groups.RemoveFromGroupAsync(connectionId, groupName);
        return true;
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
        
        var offerChangeGroups = _marketHubGroupProvider.GetAllNftOfferChangeGroup();
        foreach (var group in offerChangeGroups)
        {
            await TryRemoveFromGroupAsync(Context.ConnectionId, group);
        }
    
        await base.OnDisconnectedAsync(exception);
    }
}