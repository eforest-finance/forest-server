using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT;

public class ISeedOwnedSymbolAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly ISeedOwnedSymbolAppService _seedOwnedSymbolAppService;

    public ISeedOwnedSymbolAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _seedOwnedSymbolAppService = GetRequiredService<ISeedOwnedSymbolAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildMockISeedOwnedSymboProvider());
    }

    [Fact]
    public async Task GetSeedOwnedSymbolsAsync_ShouldBe3()
    {
        var res = await
            _seedOwnedSymbolAppService.GetSeedOwnedSymbolsAsync(new GetSeedOwnedSymbols
            {
                Address = "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk",
                Symbol = "bbb",
                MaxResultCount = 10,
                SkipCount = 0
            });
        TestOutputHelper.WriteLine(JsonConvert.SerializeObject(res));
        res.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task GetSeedOwnedSymbolsAsync_ShouldBe0()
    {
        var res = await
            _seedOwnedSymbolAppService.GetSeedOwnedSymbolsAsync(new GetSeedOwnedSymbols
            {
                Address = "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk",
                Symbol = "aaa",
                MaxResultCount = 10,
                SkipCount = 0
            });
        TestOutputHelper.WriteLine(JsonConvert.SerializeObject(res));
        res.TotalCount.ShouldBe(0);
    }

    private static ISeedOwnedSymboProvider BuildMockISeedOwnedSymboProvider()
    {
        var result =
            "{\"TotalRecordCount\":3,\"IndexerSeedOwnedSymbolList\":[{\"Id\":\"AELF-SEED-135\",\"Symbol\":\"NZC-0\",\"SeedSymbol\":\"SEED-135\",\"Issuer\":\"2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk\",\"IsBurnable\":false,\"CreateTime\":\"2023-07-18T09:38:08.301054Z\",\"SeedExpTimeSecond\":1792145642,\"SeedExpTime\":\"2026-10-16T10:14:02Z\",\"Data\":null},{\"Id\":\"AELF-SEED-6\",\"Symbol\":\"WEDSJPNEWFM-0\",\"SeedSymbol\":\"SEED-6\",\"Issuer\":\"2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk\",\"IsBurnable\":true,\"CreateTime\":\"2023-07-05T02:41:04.3290037Z\",\"SeedExpTimeSecond\":1692145642,\"SeedExpTime\":\"2023-08-16T00:27:22Z\",\"Data\":null},{\"Id\":\"AELF-SEED-1\",\"Symbol\":\"XYZ-0\",\"SeedSymbol\":\"SEED-1\",\"Issuer\":\"2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk\",\"IsBurnable\":true,\"CreateTime\":\"2023-07-03T10:58:48.2332812Z\",\"SeedExpTimeSecond\":1691145642,\"SeedExpTime\":\"2023-08-04T10:40:42Z\",\"Data\":null}],\"Data\":null}";

        var mockISeedOwnedSymboProvider =
            new Mock<ISeedOwnedSymboProvider>();
        mockISeedOwnedSymboProvider.Setup(calc =>
            calc.GetSeedOwnedSymbolsIndexAsync(0,
                10,
                "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk"
                , "bbb")).ReturnsAsync(
            JsonConvert
                .DeserializeObject<
                    IndexerSeedOwnedSymbols>(
                    result));
        return mockISeedOwnedSymboProvider.Object;
    }
}