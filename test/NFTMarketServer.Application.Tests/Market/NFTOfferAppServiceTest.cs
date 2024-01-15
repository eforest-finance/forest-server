

using System.Linq;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Market;

public class NFTOfferAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly NFTOfferAppService _nftOfferAppService;

    public NFTOfferAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftOfferAppService = GetRequiredService<NFTOfferAppService>();
    }
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockIGraphQLHelper());
    }
    
    [Fact]
    public void GetNFTOffersAsyncTest()
    {
        var input = new GetNFTOffersInput
        {
            ChainId = "tDVW",
            NFTInfoId = "test-111",
            SkipCount = 1,
            MaxResultCount = 10
        };
        
        var result = _nftOfferAppService.GetNFTOffersAsync(input);
        result.Result.ShouldNotBeNull();
        result.Result.Items.Count.ShouldBe(1);
        result.Result.Items.First().FromAddress.ShouldBe("25CYb3bVT8fFYjS9SvTTE8J8WRJkQEky34EbCgaCAFHHq74UpW");
        result.Result.Items.First().Price.ShouldBe(10);
    }
    
    [Fact]
    public void GetNFTOffersAsync2Test()
    {
        var input = new GetNFTOffersInput
        {
            ChainId = "tDVW",
            NFTInfoId = "test-111",
            SkipCount = -1,
            MaxResultCount = 10
        };
        
        var result = _nftOfferAppService.GetNFTOffersAsync(input);
        result.Result.ShouldNotBeNull();
        result.Result.Items.Count.ShouldBe(0);
    }
    
    
    private IGraphQLHelper  MockIGraphQLHelper()
    {
        var resultStr = "{\"TotalRecordCount\":0,\"Data\":{\"TotalRecordCount\":1,\"IndexerNFTOfferList\":[{\"Id\":\"tDVW-JINMINGTRTT-1234-25CYb3bVT8fFYjS9SvTTE8J8WRJkQEky34EbCgaCAFHHq74UpW-2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV-1705498555\",\"ChainId\":\"tDVW\",\"From\":\"25CYb3bVT8fFYjS9SvTTE8J8WRJkQEky34EbCgaCAFHHq74UpW\",\"To\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Price\":10.0,\"Quantity\":1,\"ExpireTime\":\"2024-01-17T13:35:55Z\",\"NftInfo\":{\"Id\":\"tDVW-JINMINGTRTT-1234\",\"ChainId\":\"tDVW\",\"IssueChainId\":0,\"Symbol\":\"JINMINGTRTT-1234\",\"Issued\":0,\"TokenName\":\"amberN34\",\"TotalSupply\":1000,\"CreatorAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"CollectionSymbol\":\"JINMINGTRTT-0\",\"CollectionName\":\"amberC6\",\"CollectionId\":\"tDVW-JINMINGTRTT-0\",\"OtherOwnerListingFlag\":false,\"ListingPrice\":0.0,\"ListingQuantity\":0,\"LatestDealPrice\":0.0,\"LatestDealTime\":\"0001-01-01T00:00:00\",\"IsOfficial\":false},\"PurchaseToken\":{\"ChainId\":\"tDVW\",\"Address\":\"cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp\",\"Symbol\":\"ELF\",\"Decimals\":\"8\",\"Id\":\"tDVW-ELF\"}}]}}";
        var result = JsonConvert.DeserializeObject<IndexerNFTOffers>(resultStr);

        var mockService = new Mock<IGraphQLHelper>();
        mockService.Setup(calc =>
                calc.QueryAsync<IndexerNFTOffers>(
                    It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(result);

        return mockService.Object;
    }
    
}