using System.Linq;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Provider;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Market;

public class NFTListingAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly NFTListingAppService _nftListingAppService;
    public NFTListingAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftListingAppService = GetRequiredService<NFTListingAppService>();
    }
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockIGraphQLHelper());
    }
    
    [Fact]
    public void GetNFTListingsAsyncTest()
    {
        var input = new GetNFTListingsInput()
        {
            ChainId = "tDVW",
            Symbol = "test-111",
            SkipCount = 1,
            MaxResultCount = 10
        };
        
        var result = _nftListingAppService.GetNFTListingsAsync(input);
        result.Result.ShouldNotBeNull();
        result.Result.Items.Count.ShouldBe(1);
        result.Result.Items.First().OwnerAddress.ShouldBe("2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV");
        result.Result.Items.First().Prices.ShouldBe(10);
    }
    
    
    private IGraphQLHelper  MockIGraphQLHelper()
    {
        var resultStr = "{\"nftListingInfo\":{\"TotalCount\":1,\"Items\":[{\"Quantity\":69,\"Symbol\":\"test-111\",\"Owner\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Prices\":10.0,\"WhitelistId\":\"e164820156d6affb4547efc3b9ab4bb2cc55d2edfea9990064f5557a29eefa49\",\"StartTime\":\"2023-07-13T10:06:46.6548821Z\",\"PublicTime\":\"2023-07-13T10:06:46.6548821Z\",\"ExpireTime\":\"2024-01-12T10:06:46.6548821Z\",\"PurchaseToken\":{\"ChainId\":\"tDVW\",\"Symbol\":\"ELF\"},\"NftInfo\":{\"Id\":\"tDVW-JINMINGTRTT-5622\",\"ChainId\":\"tDVW\",\"IssueChainId\":0,\"Symbol\":\"JINMINGTRTT-5622\",\"Issuer\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Issued\":0,\"TokenName\":\"amberN5622\",\"TotalSupply\":1000,\"CreatorAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"CollectionSymbol\":\"JINMINGTRTT-0\",\"CollectionName\":\"amberC6\",\"CollectionId\":\"tDVW-JINMINGTRTT-0\",\"OtherOwnerListingFlag\":false,\"ListingPrice\":0.0,\"ListingQuantity\":0,\"LatestDealPrice\":0.0,\"LatestDealTime\":\"0001-01-01T00:00:00\",\"IsOfficial\":false},\"NftCollectionDto\":{\"Id\":\"tDVW-JINMINGTRTT-0\",\"ChainId\":\"tDVW\",\"Symbol\":\"JINMINGTRTT-0\",\"TokenName\":\"amberC6\",\"TotalSupply\":1,\"IsBurnable\":true,\"IssueChainId\":1931928,\"CreatorAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"IsOfficial\":false}}]}}";
        var result = JsonConvert.DeserializeObject<NFTListingPage>(resultStr);

        var mockService = new Mock<IGraphQLHelper>();
        mockService.Setup(calc =>
                calc.QueryAsync<NFTListingPage>(
                    It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(result);

        return mockService.Object;
    }
}