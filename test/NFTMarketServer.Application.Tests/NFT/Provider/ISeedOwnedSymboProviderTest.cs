using System.Collections.Generic;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT.Provider;

public class ISeedOwnedSymboProviderTest : NFTMarketServerApplicationTestBase
{
    private readonly ISeedOwnedSymboProvider _symboProvider;
    public ISeedOwnedSymboProviderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _symboProvider = GetRequiredService<ISeedOwnedSymboProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildMockIGraphQLHelper());
    }
    
    [Fact]
    public async void GetNFTListingsAsyncTest()
    {
        var result1 = await _symboProvider.GetSeedOwnedSymbolsIndexAsync(0,1,"","aaa");
        result1.TotalRecordCount.ShouldBe(1);
        result1.IndexerSeedOwnedSymbolList[0].Symbol.ShouldBe("aaa-0");
        
        var result2 = await _symboProvider.GetSeedOwnedSymbolsIndexAsync(0,1,"bbb","aaa");
        result2.TotalRecordCount.ShouldBe(1);
        result2.IndexerSeedOwnedSymbolList[0].Symbol.ShouldBe("aaa-0");
    }

    
    
    
    private static IGraphQLHelper BuildMockIGraphQLHelper()
    {
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();

        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<IndexerSeedOwnedSymbols>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(new IndexerSeedOwnedSymbols()
            {
                Data = new IndexerSeedOwnedSymbols()
                {
                    TotalRecordCount = 1,
                    IndexerSeedOwnedSymbolList = new List<IndexerSeedOwnedSymbol>()
                    {
                        new IndexerSeedOwnedSymbol()
                        {
                            Symbol = "aaa-0",
                            SeedSymbol = "SEED-1"
                        }
                    }
                }
            });
        
               
        
        return mockIGraphQLHelper.Object;
    }
}