using System.Collections.Generic;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Users;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Market;

public class INFTListingAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly INFTListingAppService _listingAppService;

    public INFTListingAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _listingAppService = GetRequiredService<INFTListingAppService>();
    }
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockIGraphQLHelper());
        services.AddSingleton(MockIUserAppService());
    }
    
    [Fact]
    public void GetNFTOffersAsyncTest()
    {
        var input = new GetNFTListingsInput
        {
            ChainId = "tDVW",
            Symbol = "FJPEUKMTTO-1",
            SkipCount = 1,
            MaxResultCount = 10
        };
        
        var result = _listingAppService.GetNFTListingsAsync(input);
        result.Result.ShouldNotBeNull();
        result.Result.Items.Count.ShouldBe(1);
    }
    
    private IGraphQLHelper MockIGraphQLHelper()
    {
        var resultStr =
            "{\"nftListingInfo\":{\"TotalCount\":2,\"Items\":[{\"Quantity\":10,\"Symbol\":\"JINMINGTRTT-123\",\"Owner\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Prices\":12.0,\"WhitelistId\":\"967fb68dbf76139dfc4578dd95a7787bf04e212a3260955bf38833f08e0a6c5f\",\"StartTime\":\"2023-07-14T02:49:58.6959381Z\",\"PublicTime\":\"2023-07-14T02:49:58.6959381Z\",\"ExpireTime\":\"2024-01-13T02:49:58.6959381Z\",\"PurchaseToken\":{\"ChainId\":\"tDVW\",\"Symbol\":\"ELF\"},\"NftInfo\":{\"Id\":\"tDVW-JINMINGTRTT-123\",\"ChainId\":\"tDVW\",\"IssueChainId\":0,\"Symbol\":\"JINMINGTRTT-123\",\"Issuer\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Issued\":0,\"TokenName\":\"amberN123\",\"TotalSupply\":1000,\"CreatorAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"CollectionSymbol\":\"JINMINGTRTT-0\",\"CollectionName\":\"amberC6\",\"CollectionId\":\"tDVW-JINMINGTRTT-0\",\"OtherOwnerListingFlag\":false,\"ListingPrice\":0.0,\"ListingQuantity\":0,\"LatestDealPrice\":0.0,\"LatestDealTime\":\"0001-01-01T00:00:00\",\"IsOfficial\":false},\"NftCollectionDto\":{\"Id\":\"tDVW-JINMINGTRTT-0\",\"ChainId\":\"tDVW\",\"Symbol\":\"JINMINGTRTT-0\",\"TokenName\":\"amberC6\",\"TotalSupply\":1,\"IsBurnable\":true,\"IssueChainId\":1931928,\"CreatorAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"IsOfficial\":false}}]}}";
        var result = JsonConvert.DeserializeObject<NFTListingPage>(resultStr);

        var mockService = new Mock<IGraphQLHelper>();
        mockService.Setup(calc =>
                calc.QueryAsync<NFTListingPage>(
                    It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(result);

        return mockService.Object;
    }

    private IUserAppService MockIUserAppService()
    {
        var mockService = new Mock<IUserAppService>();
        mockService.Setup(calc =>
                calc.GetAccountsAsync(
                    It.IsAny<List<string>>(),It.IsAny<string>()))
            .ReturnsAsync(new Dictionary<string, AccountDto>());

        return mockService.Object;
    }
}