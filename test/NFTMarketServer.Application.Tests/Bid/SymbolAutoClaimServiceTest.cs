using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Dealer.ContractInvoker;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Worker;

public class SymbolAutoClaimServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly IBidAppService _bidAppService;
    private readonly IContractInvokerFactory _contractInvokerFactory;

    public SymbolAutoClaimServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }


    private static IBidAppService MockBidAppService(AuctionInfoDto auctionInfoDto)
    {
        var _bidAppService = new Mock<IBidAppService>();
        _bidAppService.Setup(ser =>
                ser.GetUnFinishedSymbolAuctionInfoListAsync(It.Is<GetAuctionInfoRequestDto>(s =>
                    s.SkipCount == 0)))
            .Returns(Task.FromResult(new List<AuctionInfoDto>
            {
                auctionInfoDto
            }));
        //The second time，return empty list
        _bidAppService.Setup(ser =>
                ser.GetUnFinishedSymbolAuctionInfoListAsync(It.Is<GetAuctionInfoRequestDto>(s =>
                    s.SkipCount == 100)))
            .Returns(Task.FromResult(new List<AuctionInfoDto>
            {
            }));
        return _bidAppService.Object;
    }


    [Fact]
    public async Task SyncSymbolClaim_AuctionInfo_Not_Empty()
    {
        var auctionInfoDto = new AuctionInfoDto
        {
            Id = "1",
            BlockHeight = 1000L,
            EndTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow) - 60000
        };

        var _bidAppService = MockBidAppService(auctionInfoDto);
        var _contractInvokerFactory = Substitute.For<IContractInvokerFactory>();

        var _symbolAutoClaimService = new SymbolAutoClaimService(
            ServiceProvider.GetRequiredService<ILogger<SymbolAutoClaimService>>(), _bidAppService,
            _contractInvokerFactory);

        //Act
        await _symbolAutoClaimService.SyncSymbolClaimAsync();
        
        //Assert 
        Assert.Equal(AuctionFinishType.UnFinished, auctionInfoDto.FinishIdentifier);
    }
}