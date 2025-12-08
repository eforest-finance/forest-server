using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Common;
using NFTMarketServer.Seed;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Tokens;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Hubs;

public class NewAuctionInfoHandler : IConsumer<NewIndexEvent<AuctionInfoDto>>, ITransientDependency
{
    private const decimal MinMarkupDenominator = 10000;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly ILogger<NewAuctionInfoHandler> _logger;
    private readonly IMarketHubGroupProvider _marketHubGroupProvider;
    private readonly ITokenAppService _tokenAppService;

    public NewAuctionInfoHandler(IHubContext<MarketHub> hubContext, IMarketHubGroupProvider marketHubGroupProvider,
        ILogger<NewAuctionInfoHandler> logger, ITokenAppService tokenAppService)
    {
        _hubContext = hubContext;
        _marketHubGroupProvider = marketHubGroupProvider;
        _logger = logger;
        _tokenAppService = tokenAppService;
    }

    public async Task Consume(ConsumeContext<NewIndexEvent<AuctionInfoDto>> eventData)
    {
        var auctionInfoGroupName =
            _marketHubGroupProvider.GetMarketAuctionInfoGroupName(eventData.Message.Data.SeedSymbol);

        _logger.LogInformation(
            "ReceiveSymbolAuctionInfo: {auctionInfoGroupName}, startprice:{startprice},endTime:{endTime}",
            auctionInfoGroupName, eventData.Message.Data.StartPrice, eventData.Message.Data.EndTime);
        var auctionInfoDto = eventData.Message.Data;
        var amount = FTHelper.GetRealELFAmount(auctionInfoDto.StartPrice.Amount);
        var usdPrice =
            await _tokenAppService.GetCurrentDollarPriceAsync(auctionInfoDto.StartPrice.Symbol, amount);
        auctionInfoDto.StartUsdPrice = new TokenPriceDto
        {
            Amount = usdPrice,
            Symbol = SeedConstants.UsdSymbol
        };


        if (auctionInfoDto.FinishPrice != null)
        {
            var realElfAmount = FTHelper.GetRealELFAmount(auctionInfoDto.FinishPrice.Amount);
            var currentDollarPriceAsync =
                await _tokenAppService.GetCurrentDollarPriceAsync(auctionInfoDto.FinishPrice.Symbol, realElfAmount);
            auctionInfoDto.CurrentELFPrice = auctionInfoDto.FinishPrice.Amount;
            auctionInfoDto.CurrentUSDPrice = currentDollarPriceAsync;
            await ConvertMarkupPrice(auctionInfoDto, auctionInfoDto.FinishPrice);
        }
        else
        {
            auctionInfoDto.CurrentELFPrice = auctionInfoDto.StartPrice.Amount;
            auctionInfoDto.CurrentUSDPrice = usdPrice;
            await ConvertMarkupPrice(auctionInfoDto, auctionInfoDto.StartPrice);
        }

        await _hubContext.Clients.Group(auctionInfoGroupName).SendAsync("ReceiveSymbolAuctionInfo", auctionInfoDto);
    }


    public async Task ConvertMarkupPrice(AuctionInfoDto auctionInfoDto, TokenPriceDto tokenPriceDto)
    {
        var realElfAmount = FTHelper.GetRealELFAmount(tokenPriceDto.Amount);
        var minElfPriceMarkup = realElfAmount * (auctionInfoDto.MinMarkup / MinMarkupDenominator);
        var minDollarPriceMarkup =
            await _tokenAppService.GetCurrentDollarPriceAsync(tokenPriceDto.Symbol, minElfPriceMarkup);
        auctionInfoDto.MinElfPriceMarkup = minElfPriceMarkup;
        auctionInfoDto.MinDollarPriceMarkup = minDollarPriceMarkup;
        auctionInfoDto.CalculatorMinMarkup = auctionInfoDto.MinMarkup / MinMarkupDenominator;
    }
}