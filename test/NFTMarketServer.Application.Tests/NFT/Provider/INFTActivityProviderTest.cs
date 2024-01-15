using System;
using System.Collections.Generic;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT.Provider;

public class INFTActivityProviderTest : NFTMarketServerApplicationTestBase
{
    private readonly INFTActivityProvider _activityProvider;
    
    public INFTActivityProviderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _activityProvider = GetRequiredService<INFTActivityProvider>();
    }
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildMockIGraphQLHelper());
    }

    [Fact]
    public async void GetNFTActivityListAsyncTest()
    {   
        var result = await _activityProvider.GetNFTActivityListAsync("aaa",new List<int>{1},100,200,0,1);
        result.TotalRecordCount.ShouldBe(1);
        result.IndexerNftActivity[0].NFTInfoId.ShouldBe("aaa");
    }
    
    
    
    
    private static IGraphQLHelper BuildMockIGraphQLHelper()
    {
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();
            
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<NFTActivityIndex>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(new NFTActivityIndex
            {
                TotalRecordCount = 1,
                Data = new NFTActivityIndex()
                {
                    TotalRecordCount = 1,
                    IndexerNftActivity = new List<NFTActivityItem>()
                    {
                        new NFTActivityItem()
                        {
                            NFTInfoId = "aaa",
                            Type = NFTActivityType.Mint,
                            From = "bbb",
                            To = "ccc",
                            Amount = 1L,
                            PriceTokenInfo = new TokenInfoDto()
                            {
                                Id = "fff",
                                Symbol = "ggg",
                                BlockHash = "hash",
                                BlockHeight = 100
                            },
                            Price = new decimal(1),
                            TransactionHash = "ddd",
                            Timestamp = new DateTime()
                            
                            
                        }
                    }
                }
            });
        return mockIGraphQLHelper.Object;
    }
}