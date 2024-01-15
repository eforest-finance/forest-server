using System;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.Icon;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.File;

public class ISymbolIconAppServiceTest: NFTMarketServerApplicationTestBase
{
    private readonly ISymbolIconAppService _symbolIconAppService;

    public ISymbolIconAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _symbolIconAppService = GetRequiredService<ISymbolIconAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockIGraphQLHelper());
        services.AddSingleton(MockISymbolIconProvider());
    }

    private async Task Init()
    {
        
    }

    [Fact]
    public async void TestGetIconBySymbolAsync()
    { 
        var result = await _symbolIconAppService.GetIconBySymbolAsync("SEED-100","AAA");
        result.ShouldNotBeEmpty();
        result.ShouldBe("https://aelf.com/img/home/logo.png");

    }
    private static IGraphQLHelper MockIGraphQLHelper()
    {
        var result =
            "";
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();
            
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<Object>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(JsonConvert.DeserializeObject<Object>(result));
        return mockIGraphQLHelper.Object;
    }

    private static ISymbolIconProvider MockISymbolIconProvider()
    {
        var mockService = new Mock<ISymbolIconProvider>();
            
        mockService.Setup(cals => cals.GetIconBySymbolAsync(It.IsAny<string>()))
            .ReturnsAsync("https://aelf.com/img/home/logo.png");
        return mockService.Object;
    }
}