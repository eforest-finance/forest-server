using System;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using NFTMarketServer.Tokens;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Market;

public class ITokenAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly ITokenAppService _tokenAppService;
    public ITokenAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _tokenAppService = GetRequiredService<ITokenAppService>();
    }
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockITokenMarketDataProvider());
    }
    
    [Fact]
    public void GetNFTOffersAsyncTest()
    {
        var result = _tokenAppService.GetTokenMarketDataAsync("aelf",null);
        result.Result.ShouldNotBeNull();
        result.Result.Price.ShouldBe(new decimal(1));
    }
    [Fact]
    public void GetNFTOffersAsync2Test()
    {
        var result = _tokenAppService.GetTokenMarketDataAsync("aelf",DateTime.Now);
        result.Result.ShouldNotBeNull();
        result.Result.Price.ShouldBe(new decimal(1));
    }
    

    public ITokenMarketDataProvider MockITokenMarketDataProvider()
    {
        var mockService = new Mock<ITokenMarketDataProvider>();
        mockService.Setup(calc =>
                calc.GetPriceAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(new Decimal(1));
        mockService.Setup(calc =>
                calc.GetHistoryPriceAsync(
                    It.IsAny<string>(),It.IsAny<DateTime>()))
            .ReturnsAsync(new Decimal(1));
        return mockService.Object;

    }
    
}