using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Bid;
using NFTMarketServer.Common;
using NFTMarketServer.SymbolMarketToken.Index;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.SymbolMarketToken;

public class SymbolMarketTokenAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly ISymbolMarketTokenAppService _symbolMarketTokenAppService;
    private readonly ILocalEventBus _localEventBus;
    
    public SymbolMarketTokenAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _symbolMarketTokenAppService = GetRequiredService<ISymbolMarketTokenAppService>();
        _localEventBus = GetRequiredService<ILocalEventBus>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockIGraphQLHelper());
        services.AddSingleton(BuildMockIBidAppService());
    }

    [Fact]
    public async Task TestGetSymbolMarketTokensAsync()
    {
        var input = new GetSymbolMarketTokenInput()
        {
            AddressList = new List<string>()
            {
                "mHmRCqE4FSmHeCXj5WrbfGfXqx5LCDfVo9F1gC1G3bgMiEwq3"
            },
            SkipCount = 0,
            MaxResultCount = 10
        };
        
        var result = await _symbolMarketTokenAppService.GetSymbolMarketTokensAsync(input);
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(4);
        result.Items[0].Issuer.ShouldBe("2tnFV5RQ8UWrAsCHTzJwZzB28rtmPMEKdaXPwtRCEGPJ5r1beg");
    }

    [Fact]
    public async Task TestGetSymbolMarketTokenIssuerAsync()
    {
        var input = new GetSymbolMarketTokenIssuerInput()
        {
            IssueChainId = 1866392,
            TokenSymbol = "WUGEHHHHH"
        };
        var result = await _symbolMarketTokenAppService.GetSymbolMarketTokenIssuerAsync(input);
        result.ShouldNotBeNull();
        result.Issuer.ShouldBe("2tnFV5RQ8UWrAsCHTzJwZzB28rtmPMEKdaXPwtRCEGPJ5r1beg");
    }
    
    [Fact]
    public async Task TestGetSymbolMarketTokenExistAsync()
    {
        var input = new GetSymbolMarketTokenExistInput()
        {
            IssueChainId = "AELF",
            TokenSymbol = "WUGEHHHHH"
        };
        var result = await _symbolMarketTokenAppService.GetSymbolMarketTokenExistAsync(input);
        result.ShouldNotBeNull();
        result.Exist.ShouldBeTrue();
    }
    
    private static IGraphQLHelper MockIGraphQLHelper()
    {
        var result1 =
            "{\"Data\":{\"TotalRecordCount\":4,\"IndexerSymbolMarketTokenList\":[{\"Symbol\":\"WUGEHHHHH\",\"TokenName\":\"WUGEHHHHH\",\"Issuer\":\"2tnFV5RQ8UWrAsCHTzJwZzB28rtmPMEKdaXPwtRCEGPJ5r1beg\",\"IssueChainId\":1866392,\"Decimals\":0,\"TotalSupply\":10,\"Supply\":1,\"Issued\":1,\"IssueManagerList\":[\"2r7fwM7z3LczMbGDxn7DDpNEBqbytCmEh7RLK3uMzraHqYkaM7\"]}]}}";
        var result2 =  "{\"Data\":{\"SymbolMarketTokenIssuer\":\"2tnFV5RQ8UWrAsCHTzJwZzB28rtmPMEKdaXPwtRCEGPJ5r1beg\"}}";
        var result3 =
            "{\"Data\":{\"Symbol\":\"WUGEHHHHH\",\"IssueChain\":\"AELF\",\"TokenName\":\"WUGEHHHHH\"}}";
        
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();

        mockIGraphQLHelper.Setup(cals =>
                cals.QueryAsync<IndexerSymbolMarketTokens>(It.Is<GraphQLRequest>(x => x.Query.IndexOf("symbolMarketTokens")>0)))
            .ReturnsAsync(JsonConvert.DeserializeObject<IndexerSymbolMarketTokens>(result1));
        mockIGraphQLHelper.Setup(cals =>
                cals.QueryAsync<IndexerSymbolMarketIssuer>(It.Is<GraphQLRequest>(x => x.Query.IndexOf("symbolMarketTokenIssuer")>0)))
            .ReturnsAsync(JsonConvert.DeserializeObject<IndexerSymbolMarketIssuer>(result2));
        mockIGraphQLHelper.Setup(cals =>
                cals.QueryAsync<IndexerSymbolMarketTokenExist>(It.Is<GraphQLRequest>(x =>
                    x.Query.IndexOf("symbolMarketTokenExist") > 0)))
            .ReturnsAsync(JsonConvert.DeserializeObject<IndexerSymbolMarketTokenExist>(result3));
        
        return mockIGraphQLHelper.Object;
    }
    
    private static IBidAppService BuildMockIBidAppService()
    {
        var mockIBidAppService =
            new Mock<IBidAppService>();
        return mockIBidAppService.Object;
    }

}