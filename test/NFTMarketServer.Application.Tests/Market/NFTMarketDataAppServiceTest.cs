using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Market;

public class NFTMarketDataAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly NFTMarketDataAppService _nftMarketDataAppService;

    public NFTMarketDataAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftMarketDataAppService = GetRequiredService<NFTMarketDataAppService>();
    }
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockIGraphQLClient());
        services.AddSingleton(MockIGraphQLHelper());
    }

    [Fact]
    public void GetMarketDataAsyncTest()
    {
        var input = new GetNFTInfoMarketDataInput()
        {
            NFTInfoId = "tDVW-JINMINGTUESSTT-1000"
        };
        
        var result = _nftMarketDataAppService.GetMarketDataAsync(input);
        result.Result.ShouldNotBeNull();
        result.Result.Items.Count.ShouldBe(1);
        result.Result.Items.First().Price.ShouldBe(new decimal(1.111));
        result.Result.Items.First().Timestamp.ShouldBe(1111111);
    }


    private IGraphQLHelper MockIGraphQLHelper()
    {
        var result = new IndexerNFTInfoMarketDatas()
        {
            TotalRecordCount = 1,
            indexerNftInfoMarketDatas = new List<IndexerNFTInfoMarketData>()
            {
                new()
                {
                    Price = new decimal(1.111),
                    Timestamp = 1111111
                }
            },
            Data = new IndexerNFTInfoMarketDatas()
            {
                TotalRecordCount = 1,
                indexerNftInfoMarketDatas = new List<IndexerNFTInfoMarketData>()
                {
                    new()
                    {
                        Price = new decimal(1.111),
                        Timestamp = 1111111
                    }
                }
            }
        };
        var result2 = JsonConvert.DeserializeObject<IndexerNFTInfoMarketDatas>(
            "{\n  \"TotalRecordCount\": 0,\n  \"Data\": {\n    \"TotalRecordCount\": 1,\n    \"indexerNftInfoMarketDatas\": [\n      {\n        \"Price\": 1300000000.0,\n        \"Timestamp\": 1690934400000\n      }\n    ]\n  }\n}");
        var mockService = new Mock<IGraphQLHelper>();
        mockService.Setup(calc =>
            calc.QueryAsync<IndexerNFTInfoMarketDatas>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(
                result);

        return mockService.Object;
    }
    private IGraphQLClient  MockIGraphQLClient()
    {
        var result = new IndexerNFTInfoMarketDatas()
        {
            TotalRecordCount = 1,
            indexerNftInfoMarketDatas = new List<IndexerNFTInfoMarketData>()
            {
                new()
                {
                    Price = new decimal(1.111),
                    Timestamp = 1111111
                }
            },
            Data = new IndexerNFTInfoMarketDatas()
            {
                TotalRecordCount = 1,
                indexerNftInfoMarketDatas = new List<IndexerNFTInfoMarketData>()
                {
                    new()
                    {
                        Price = new decimal(1.111),
                        Timestamp = 1111111
                    }
                }
            }
        };
       
        var mockService = new Mock<IGraphQLClient>();
        mockService.Setup(calc =>
                calc.SendQueryAsync<IndexerNFTInfoMarketDatas>(
                    It.IsAny<GraphQLRequest>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GraphQLResponse<IndexerNFTInfoMarketDatas>()
                {
                    Data = result
                });

        return mockService.Object;
    }
}