using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NFTMarketServer.Whitelist.Dto;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Whitelist;

public partial class WhitelistAppServiceTests : NFTMarketServerApplicationTestBase
{
    private readonly IWhitelistAppService _whitelistAppService;

    public WhitelistAppServiceTests(ITestOutputHelper testOutputHelper) : base(null)
    {
        _whitelistAppService = GetRequiredService<IWhitelistAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetMockWhitelistProvider());
    }

    [Fact]
    public async Task GetWhitelistByHashAsyncTest()
    {
        var input = new GetWhitelistByHashDto()
        {
            ChainId = "testChainId",
            WhitelistHash = "testHash",
        };
        var result = await _whitelistAppService.GetWhitelistByHashAsync(input);
        result.ShouldNotBeNull();
        result.ChainId.ShouldBe("tDVW");
        result.WhitelistHash.ShouldBe("test");
        result.ProjectId.ShouldBe("testProject");
        result.Creator.ShouldBe("testcreator");
        result.Remark.ShouldBe("test");
    }

    [Fact]
    public async Task GetExtraInfoListAsyncTest()
    {
        var input = new GetWhitelistExtraInfoListDto
        {
            MaxResultCount = 100,
            SkipCount = 0,
            ChainId = "testChainId",
            ProjectId = "testProjectId",
            WhitelistHash = "testWhitelistHash",
            CurrentAddress = "testAddress",
            TagHash = "testTagHash",
        };
        var result = await _whitelistAppService.GetWhitelistExtraInfoListAsync(input);
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(1L);
        result.Items[0].ChainId.ShouldBe("tDVW");
        result.Items[0].Address.ShouldBe("testAddress");
        result.Items[0].WhitelistInfo.ShouldNotBeNull();
        result.Items[0].WhitelistInfo.ChainId.ShouldBe("tDVW");
    }

    [Fact]
    public async Task GetManagerListAsyncTest()
    {
        var input = new GetWhitelistManagerListDto
        {
            MaxResultCount = 100,
            SkipCount = 0,
            ChainId = "testChainId",
            ProjectId = "testProjectId",
            WhitelistHash = "testWhitelistHash",
            Address = "testAddress",
        };
        var result = await _whitelistAppService.GetWhitelistManagerListAsync(input);
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(1L);
        result.Items.ShouldNotBeNull();
        result.Items[0].ChainId.ShouldBe("tDVW");
        result.Items[0].Manager.ShouldBe("testAddress");
        result.Items[0].WhitelistInfo.ChainId.ShouldBe("tDVW");
        result.Items[0].WhitelistInfo.WhitelistHash.ShouldBe("test");
        
    }

    [Fact]
    public async Task GetTagListAsyncTest()
    {
        var input = new GetTagInfoListDto
        {
            MaxResultCount = 100,
            SkipCount = 0,
            ChainId = "AELF",
            ProjectId = "testProjectId",
            WhitelistHash = "testWhitelistHash",
            PriceMin = 0,
            PriceMax = 10000
        };
        var result = await _whitelistAppService.GetWhitelistTagInfoListAsync(input);
        result.ShouldNotBeNull();
        result.Items.ShouldNotBeNull();
        result.Items[0].AddressCount.ShouldBe(1);
        result.Items[0].TagHash.ShouldBe("testWhitelistHash");
        result.Items[0].Name.ShouldBe("testName");
        result.Items[0].PriceTagInfo.ShouldNotBeNull();
        result.Items[0].PriceTagInfo.Symbol.ShouldBe("testSymbol");
        result.Items[0].PriceTagInfo.Price.ShouldBe(new decimal(1.111));
        result.Items[0].WhitelistInfo.ShouldNotBeNull();
        result.Items[0].WhitelistInfo.ChainId.ShouldBe("tDVW");

    }
}