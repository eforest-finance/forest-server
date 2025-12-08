using System.Collections.Generic;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.Common;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Seed.Provider;

public class ISeedProviderTest : NFTMarketServerApplicationTestBase
{
    private readonly ISeedProvider _seedProvider;
    public ISeedProviderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _seedProvider = GetRequiredService<ISeedProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(BuildMockIGraphQLHelper());
    }
    
    [Fact]
    public async void GetSpecialSeedsAsyncTest()
    {
        var result = await _seedProvider.GetSpecialSeedsAsync(new QuerySpecialListInput()
        {
            ChainIds = new List<string>(){"aaa","bbb"},
            PriceMin = 200,
            PriceMax = 1000
        });
        result.TotalRecordCount.ShouldBe(1);
        result.IndexerSpecialSeedList[0].Symbol.ShouldBe("aaa-0");
    }
    
    private static IGraphQLHelper BuildMockIGraphQLHelper()
    {
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();

        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<IndexerSpecialSeeds>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(new IndexerSpecialSeeds()
            {
                Data = new IndexerSpecialSeeds()
                {
                    TotalRecordCount = 1,
                    IndexerSpecialSeedList = new List<SpecialSeedItem>()
                    {
                        new SpecialSeedItem()
                        {
                            Symbol = "aaa-0",
                            SeedName = "SEED-aaa-0",
                            TokenPrice = new TokenPriceDto()
                            {
                                Amount = 300,
                                Symbol = "ELF"
                            }
                        },
                        new SpecialSeedItem()
                        {
                            Symbol = "ELK",
                            SeedName = "SEED-ELK",
                            TokenPrice = new TokenPriceDto()
                            {
                                Amount = 600,
                                Symbol = "ELF"
                            }
                        }
                    }
                }
            });
        return mockIGraphQLHelper.Object;
    }
}