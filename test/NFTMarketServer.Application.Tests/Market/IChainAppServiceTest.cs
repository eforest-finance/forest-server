using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT;

public class IChainAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly IChainAppService _chainAppService;
    public IChainAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _chainAppService = GetRequiredService<IChainAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildMockIChainAppService());
    }

    [Fact]
    public async Task TestCalculatePercentage()
    {
        var a = new Decimal(0.01);
        var b = new Decimal(101);
        var result = PercentageCalculatorHelper.CalculatePercentage(a,b);
        result.Equals(0.9999);
    }
    
    [Fact]
    public async Task GetListAsyncTest()
    {
        var res = await _chainAppService.GetListAsync();
        TestOutputHelper.WriteLine(JsonConvert.SerializeObject(res));
        res.ShouldNotBeNull();
        res[0].ShouldBe("AELF");
    }

    private static IChainAppService BuildMockIChainAppService()
    {
        var result = "";
        var mockIChainAppService = new Mock<IChainAppService>();
        mockIChainAppService.Setup(calc => calc.GetListAsync()).ReturnsAsync(new []{"AELF"});

        return mockIChainAppService.Object;
    }
}