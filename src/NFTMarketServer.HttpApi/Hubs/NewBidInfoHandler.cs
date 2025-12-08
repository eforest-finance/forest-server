using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using NFTMarketServer.Seed;
using NFTMarketServer.Tokens;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Hubs;

public class NewBidInfoHandler : IConsumer<NewIndexEvent<BidInfoDto>>, ITransientDependency
{
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly IMarketHubGroupProvider _marketHubGroupProvider;
    private readonly ILogger<NewBidInfoHandler> _logger;
    private readonly ITokenAppService _tokenAppService;
    private readonly IBidAppService _bidAppService;
    private const decimal MinMarkupDenominator = 10000;
    private readonly IChainAppService _chainAppService;
  

    public NewBidInfoHandler(IHubContext<MarketHub> hubContext, IMarketHubGroupProvider marketHubGroupProvider,
                             ILogger<NewBidInfoHandler> logger, ITokenAppService tokenAppService, IBidAppService bidAppService, IChainAppService chainAppService)
    {
        _hubContext = hubContext;
        _marketHubGroupProvider = marketHubGroupProvider;
        _logger = logger;
        _tokenAppService = tokenAppService;
        _bidAppService = bidAppService;
        _chainAppService = chainAppService;
    }

    public async Task Consume(ConsumeContext<NewIndexEvent<BidInfoDto>> eventData)
    {
        var bidInfoGroupName =
            _marketHubGroupProvider.GetMarketBidInfoGroupName(eventData.Message.Data.SeedSymbol);
        _logger.LogInformation(
            "ReceiveSymbolBidInfo: {bidInfoGroupName}, Bidder:{Bidder},Price:{Price},BidTime:{BidTime} start time {time}",
            bidInfoGroupName, eventData.Message.Data.Bidder, eventData.Message.Data.PriceAmount,
            eventData.Message.Data.BidTime, DateTime.Now.ToString());

        var bidInfoDto = eventData.Message.Data;
        var price = Convert.ToDecimal(bidInfoDto.PriceAmount);
        var dollarStartTime = DateTime.Now;
        _logger.LogInformation(
            "ReceiveSymbolBidInfo:SeedSymbol,convert dollar start time;{time}:{SeedSymbol}", bidInfoDto.SeedSymbol, dollarStartTime.ToString());
        var realElfAmount = FTHelper.GetRealELFAmount(price);
        var usdPrice =
            await _tokenAppService.GetCurrentDollarPriceAsync(bidInfoDto.PriceSymbol, realElfAmount);
        var dollarEndTime = DateTime.Now;
        _logger.LogInformation(
            "ReceiveSymbolBidInfo:SeedSymbol,convert dollar end time:{time}:{SeedSymbol}", bidInfoDto.SeedSymbol, dollarEndTime.ToString());
        
        _logger.LogInformation(
            "ReceiveSymbolBidInfo:SeedSymbol,convert dollar cost time:{time}:{SeedSymbol}", bidInfoDto.SeedSymbol, (dollarEndTime-dollarStartTime).TotalMilliseconds);
        bidInfoDto.PriceUsdAmount = usdPrice;
        bidInfoDto.PriceUsdSymbol = SeedConstants.UsdSymbol;
        if (!bidInfoDto.Bidder.IsNullOrEmpty())
        {
            var chainId = "tDVV";
            var chainIds = await _chainAppService.GetListAsync();
            if (chainIds != null && chainIds.Length > 1)
            {
                chainId = chainIds[1];
            }

            bidInfoDto.Bidder = "ELF_" + bidInfoDto.Bidder + "_" + chainId;
        }

        var MinMarkupStart = DateTime.Now;

        _logger.LogInformation(
            "ReceiveSymbolBidInfo:SeedSymbol,convert MinMarkup end time:{time}:{SeedSymbol}", bidInfoDto.SeedSymbol, MinMarkupStart.ToString());
        var symbolAuctionInfoAsync = await _bidAppService.GetSymbolAuctionInfoAsync(bidInfoDto.SeedSymbol);
        if (symbolAuctionInfoAsync != null)
        {
            var elfAmount = FTHelper.GetRealELFAmount(bidInfoDto.PriceAmount);
            var minElfPriceMarkup = elfAmount * (symbolAuctionInfoAsync.MinMarkup / MinMarkupDenominator);
            var minDollarPriceMarkup = await _tokenAppService.GetCurrentDollarPriceAsync(bidInfoDto.PriceSymbol, minElfPriceMarkup);
            bidInfoDto.MinElfPriceMarkup = minElfPriceMarkup;
            bidInfoDto.MinDollarPriceMarkup = minDollarPriceMarkup;
            bidInfoDto.CalculatorMinMarkup = symbolAuctionInfoAsync.MinMarkup / MinMarkupDenominator;
        }
        var MinMarkupEnd = DateTime.Now;
        _logger.LogInformation(
            "ReceiveSymbolBidInfo:SeedSymbol,convert MinMarkup end time:{time}:{SeedSymbol}", bidInfoDto.SeedSymbol, MinMarkupEnd.ToString());

        _logger.LogInformation(
            "ReceiveSymbolBidInfo:SeedSymbol,convert MinMarkup cost  time:{time}:{SeedSymbol}", bidInfoDto.SeedSymbol, (MinMarkupEnd - MinMarkupStart).TotalMilliseconds);
        await _hubContext.Clients.Group(bidInfoGroupName).SendAsync("ReceiveSymbolBidInfo", bidInfoDto);
    }
}