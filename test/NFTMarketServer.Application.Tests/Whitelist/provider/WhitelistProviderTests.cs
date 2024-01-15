using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.Common;
using NFTMarketServer.Whitelist.Provider;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Whitelist.provider;

[Collection(NFTMarketServerTestConsts.CollectionDefinitionName)]
public class WhitelistProviderTests : NFTMarketServerApplicationTestBase
{
    private IWhitelistProvider _whitelistProvider;

    public WhitelistProviderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _whitelistProvider = GetRequiredService<IWhitelistProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetMockIGraphQLHelper());
    }

    [Fact]
    public async void GetWhitelistByHashAsyncTest()
    {
        await _whitelistProvider.GetWhitelistByHashAsync("AELF", "AAA");
    }

    [Fact]
    public async void GetWhitelistExtraInfoListAsyncTest()
    {
        await _whitelistProvider.GetWhitelistExtraInfoListAsync("AELF", "testProject", "AAA", 100, 0);
    }

    [Fact]
    public async void GetWhitelistManagerListAsyncTest()
    {
        await _whitelistProvider.GetWhitelistManagerListAsync("AELF", "testProject", "AAA", "BBB", 100, 0);
    }

    [Fact]
    public async void GetWhitelistTagInfoListAsyncTest()
    {
        await _whitelistProvider.GetWhitelistTagInfoListAsync("AELF", "testProject", "AAA", 1000, 0, 100, 0);
    }

    private IGraphQLHelper GetMockIGraphQLHelper()
    {
        var mockHelper = new Mock<IGraphQLHelper>();
        return mockHelper.Object;
    }
}