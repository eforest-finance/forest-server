using System.Collections.Generic;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace NFTMarketServer.NFT.Provider;

[Collection(NFTMarketServerTestConsts.CollectionDefinitionName)]
public class INFTInfoProviderTests : NFTMarketServerApplicationTestBase
{
    private INFTInfoProvider _nftInfoProvider;

    public INFTInfoProviderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftInfoProvider = GetRequiredService<INFTInfoProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildMockIGraphQLHelper());
    }

    [Fact]
    public async void GetNFTInfoIndexsAsyncTest()
    {
        var result0 = await _nftInfoProvider.GetNFTInfoIndexsAsync(0,1,"","1",0,0,0,"","",new List<string>());
        result0.ShouldNotBeNull();
        result0.IndexerNftInfos[0].Id.ShouldBe("tDVW-JINMINGTRT-1");
        
        var result1 = await _nftInfoProvider.GetNFTInfoIndexsAsync(0,1,"","1",0,0,1,"","",new List<string>());
        result1.ShouldNotBeNull();
        result1.IndexerNftInfos[0].Id.ShouldBe("tDVW-JINMINGTRT-1");
        
        var result2 = await _nftInfoProvider.GetNFTInfoIndexsAsync(0,1,"","1",0,0,2,"","",new List<string>());
        result2.ShouldNotBeNull();
        result2.IndexerNftInfos[0].Id.ShouldBe("tDVW-JINMINGTRT-1");
    }
    
    [Fact]
    public async void GetNFTInfoIndexAsyncTest()
    {
        var result0 = await _nftInfoProvider.GetNFTInfoIndexAsync("testNFTInfoId","2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV");
        result0.ShouldNotBeNull();
        result0.Id.ShouldBe("tDVW-JINMINGTRT-1");
        var result1 = await _nftInfoProvider.GetNFTInfoIndexAsync("testNFTInfoId","");
        result1.ShouldNotBeNull();
        result1.Id.ShouldBe("tDVW-JINMINGTRT-1");
    }
    [Fact]
    public async void GetNFTCollectionSymbolAsyncTest()
    {
        var result = await _nftInfoProvider.GetNFTCollectionSymbolAsync("JINMINGTRT-1");
        result.Symbol.ShouldBe("JINMINGTRT-1");
    }
    [Fact]
    public async void GetNFTSymbolAsyncTest()
    {
        var result = await _nftInfoProvider.GetNFTSymbolAsync("JINMINGTRT-1");
        result.Symbol.ShouldBe("JINMINGTRT-1");
    }
    
    private static IGraphQLHelper BuildMockIGraphQLHelper()
    {
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();
            
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<IndexerNFTInfoMarketDatas>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(new IndexerNFTInfoMarketDatas
            {
                Data = new IndexerNFTInfoMarketDatas
                {
                    TotalRecordCount = 0,
                    indexerNftInfoMarketDatas = new List<IndexerNFTInfoMarketData>
                    {
                        new IndexerNFTInfoMarketData
                        {
                            Price = 1300000000,
                            Timestamp = 1689638400000,
                            Data = null
                        }
                    }
                }
            });
        var result2 = "{\"TotalRecordCount\":0,\"IndexerNftInfos\":null,\"Data\":{\"TotalRecordCount\":1,\"IndexerNftInfos\":[{\"Id\":\"tDVW-JINMINGTRT-1\",\"ChainId\":\"tDVW\",\"IssueChainId\":1931928,\"Symbol\":\"JINMINGTRT-1\",\"Issuer\":null,\"proxyIssuerAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Owner\":null,\"Issued\":0,\"TokenName\":\"amberN1\",\"TotalSupply\":1,\"WhitelistId\":null,\"CreatorAddress\":null,\"ImageUrl\":null,\"CollectionSymbol\":null,\"CollectionName\":null,\"CollectionId\":\"tDVW-JINMINGTRT-0\",\"OtherOwnerListingFlag\":false,\"ListingId\":null,\"ListingAddress\":null,\"ListingPrice\":0.0,\"ListingQuantity\":0,\"ListingEndTime\":null,\"LatestListingTime\":null,\"LatestDealPrice\":0.0,\"LatestDealTime\":\"0001-01-01T00:00:00\",\"ListingToken\":null,\"LatestDealToken\":null,\"PreviewImage\":null,\"File\":null,\"FileExtension\":null,\"Description\":null,\"IsOfficial\":false,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"\\\"9fd7abe078427234f793f4cdac834874\\\"\",\"Data\":null}],\"Data\":null}],\"Data\":null}}";
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<IndexerNFTInfos>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(JsonConvert.DeserializeObject<IndexerNFTInfos>(result2));
        var result3 = "{\"IssueChainId\":0,\"Issued\":0,\"TotalSupply\":0,\"OtherOwnerListingFlag\":false,\"ListingPrice\":0.0,\"ListingQuantity\":0,\"LatestDealPrice\":0.0,\"LatestDealTime\":\"0001-01-01T00:00:00\",\"IsOfficial\":false,\"Data\":{\"Id\":\"tDVW-JINMINGTRT-1\",\"ChainId\":\"tDVW\",\"IssueChainId\":1931928,\"Symbol\":\"JINMINGTRT-1\",\"proxyIssuerAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Owner\":\"\",\"Issued\":0,\"TokenName\":\"amberN1\",\"TotalSupply\":1,\"CollectionId\":\"tDVW-JINMINGTRT-0\",\"OtherOwnerListingFlag\":false,\"ListingPrice\":0.0,\"ListingQuantity\":0,\"LatestDealPrice\":0.0,\"LatestDealTime\":\"0001-01-01T00:00:00\",\"IsOfficial\":false,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"\\\"9fd7abe078427234f793f4cdac834874\\\"\"}]}}";
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<IndexerNFTInfo>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(JsonConvert.DeserializeObject<IndexerNFTInfo>(result3));
        var result4 = "";
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<IndexerSymbol>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(new IndexerSymbol()
            {
                Data = new IndexerSymbol()
                {
                    Symbol = "JINMINGTRT-1"   
                }
            });
        
        return mockIGraphQLHelper.Object;
    }
}