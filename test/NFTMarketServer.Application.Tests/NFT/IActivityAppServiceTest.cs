using System.Collections.Generic;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Activity;
using NFTMarketServer.Activity.Index;
using NFTMarketServer.Common;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT;

public class IActivityAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly IActivityAppService _activityAppService;

    public IActivityAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _activityAppService = GetRequiredService<IActivityAppService>();
    }
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockIGraphQLHelper());
    }

    [Fact]
    public async void TestGetListAsync()
    {
        var input = new NFTMarketServer.Activity.GetActivitiesInput()
        {
            Address = "EPxUCDZPHpzMvqa9Vu6hLjy5UBWDzHPxhZpVmPURUErqhMZRu",
            Types = new List<SymbolMarketActivityType>()
            {
                SymbolMarketActivityType.Buy
            }
        };
        var result = await _activityAppService.GetListAsync(input);
        result.ShouldNotBe(null);
        result.TotalCount.ShouldBe(22);
        result.Items[0].Symbol.ShouldBe("OJHKUYSHDKQYSSUHD-0");
    }
    
    private static IGraphQLHelper MockIGraphQLHelper()
    {
        var result =
            "{\n  \"TotalRecordCount\": 0,\n  \"Data\": {\n    \"TotalRecordCount\": 22,\n    \"IndexerActivityList\": [\n      {\n        \"TransactionDateTime\": \"2023-11-22T02:15:44.5487377Z\",\n        \"Symbol\": \"OJHKUYSHDKQYSSUHD-0\",\n        \"Type\": 1,\n        \"Price\": 300000000.0,\n        \"PriceSymbol\": \"ELF\",\n        \"TransactionFee\": 38885000.0,\n        \"TransactionFeeSymbol\": \"ELF\",\n        \"TransactionId\": \"14cb845e6bb7bd27d82224254d667e9f59eaf5a8133fc50c0d31ba2a8e2f10eb\"\n      }\n    ]\n  }\n}";
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();
            
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<IndexerActivities>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(JsonConvert.DeserializeObject<IndexerActivities>(result));
        return mockIGraphQLHelper.Object;
    }
}