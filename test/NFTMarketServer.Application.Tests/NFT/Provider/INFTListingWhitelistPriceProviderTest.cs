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

public class INFTListingWhitelistPriceProviderTest : NFTMarketServerApplicationTestBase
{
    private readonly INFTListingWhitelistPriceProvider _listingWhitelistPriceProvider;
    public INFTListingWhitelistPriceProviderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _listingWhitelistPriceProvider = GetRequiredService<INFTListingWhitelistPriceProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildMockIGraphQLHelper());
    }
    
    [Fact]
    public async void GetNFTListingsAsyncTest()
    {
        var result = await _listingWhitelistPriceProvider.GetNFTListingWhitelistPricesAsync("aa",new List<string>(){"11","22"});
        result.Count.ShouldBe(1);
        result[0].Owner.ShouldBe("ccc");
    }
    
    
    private static IGraphQLHelper BuildMockIGraphQLHelper()
    {
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();

        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<ListingPriceData>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(new ListingPriceData
            {
             nftListingWhitelistPrices   = new PagedResultDto<IndexerListingWhitelistPrice>()
             {
                 TotalCount = 1,
                 Items = new List<IndexerListingWhitelistPrice>()
                 {
                     new IndexerListingWhitelistPrice()
                     {
                         ListingId = "aaa",
                         NftInfoId = "bbb",
                         Owner = "ccc"
                     }
                 }
             }
            });
               
        
        return mockIGraphQLHelper.Object;
    }
}