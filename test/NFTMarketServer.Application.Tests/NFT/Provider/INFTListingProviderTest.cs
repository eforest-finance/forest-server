using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.Market;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT.Provider;

public class INFTListingProviderTest : NFTMarketServerApplicationTestBase
{
    private readonly INFTListingProvider _nftListingProvider;
    public INFTListingProviderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftListingProvider = GetRequiredService<INFTListingProvider>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildMockIGraphQLHelper());
    }

    [Fact]
    public async void GetNFTListingsAsyncTest()
    {
        var dto = new GetNFTListingsDto()
        {
            ChainId = "aa",
            Symbol = "bb",
            Address = "cc",
            SkipCount = 0,
            MaxResultCount = 1
        };
        var result = await _nftListingProvider.GetNFTListingsAsync(dto);
        result.TotalCount.ShouldBe(1);
        result.Items[0].Symbol.ShouldBe("HRKVUQCH-2131");
    }
    
    
    private static IGraphQLHelper BuildMockIGraphQLHelper()
    {
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();
        
        var result1 = "{\"nftListingInfo\":{\"TotalCount\":1,\"Items\":[{\"Quantity\":1,\"Symbol\":\"HRKVUQCH-2131\",\"Owner\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Prices\":1234567891.12,\"StartTime\":\"2023-08-09T09:03:54.0763319Z\",\"PublicTime\":\"2023-08-09T09:03:54.0763319Z\",\"ExpireTime\":\"2024-02-08T09:03:54.0763319Z\",\"PurchaseToken\":{\"ChainId\":\"tDVW\",\"Symbol\":\"ELF\"},\"NftInfo\":{\"Id\":\"tDVW-HRKVUQCH-2131\",\"ChainId\":\"tDVW\",\"IssueChainId\":0,\"Symbol\":\"HRKVUQCH-2131\",\"Issuer\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Issued\":0,\"TokenName\":\"name111\",\"TotalSupply\":123,\"CreatorAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"CollectionSymbol\":\"HRKVUQCH-0\",\"CollectionName\":\"amberC08094\",\"CollectionId\":\"tDVW-HRKVUQCH-0\",\"OtherOwnerListingFlag\":false,\"ListingPrice\":0.0,\"ListingQuantity\":0,\"LatestDealPrice\":0.0,\"LatestDealTime\":\"0001-01-01T00:00:00\",\"IsOfficial\":false},\"NftCollectionDto\":{\"Id\":\"tDVW-HRKVUQCH-0\",\"ChainId\":\"tDVW\",\"Symbol\":\"HRKVUQCH-0\",\"TokenName\":\"amberC08094\",\"TotalSupply\":1,\"IsBurnable\":true,\"IssueChainId\":1931928,\"CreatorAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"IsOfficial\":false}}]}}";
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<NFTListingPage>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(JsonConvert.DeserializeObject<NFTListingPage>(result1));
        
        return mockIGraphQLHelper.Object;
    }
    
}