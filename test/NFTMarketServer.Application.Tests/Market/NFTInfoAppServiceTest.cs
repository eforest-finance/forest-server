using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Market;

public class NFTInfoAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly NFTInfoAppService _nftInfoAppService;

    public NFTInfoAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftInfoAppService = GetRequiredService<NFTInfoAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(buildNftInfoProvider());
        services.AddSingleton(MockIUserAppService());
        services.AddSingleton(MockITokenAppService());
    }

    [Fact]
    public async Task GetSymbolInfoAsyncTest()
    {
        GetSymbolInfoInput symbolInfoInput = new GetSymbolInfoInput
        {
            Symbol = "AXZ-0",
        };
        var res = await _nftInfoAppService.GetSymbolInfoAsync(symbolInfoInput);

        res.Exist.Equals(true);
    }

    private INFTInfoProvider buildNftInfoProvider()
    {
        var result = "{\"Exist\":true}";

        String Symbol = "AXZ-0";

        var provider = new Mock<INFTInfoProvider>();

        provider.Setup(calc =>
            calc.GetNFTCollectionSymbolAsync(Symbol)).ReturnsAsync(
            JsonConvert.DeserializeObject<IndexerSymbol>(result));

        provider.Setup(calc =>
            calc.GetNFTSymbolAsync(Symbol)).ReturnsAsync(
            JsonConvert.DeserializeObject<IndexerSymbol>(result));

        return provider.Object;
    }


    private ITokenAppService MockITokenAppService()
    {
        var result = "{\"Exist\":true}";

        String Symbol = "AXZ-0";

        var mockService = new Mock<ITokenAppService>();

        return mockService.Object;
    }

    private IUserAppService MockIUserAppService()
    {
        var mockService = new Mock<IUserAppService>();

        return mockService.Object;
    }
}