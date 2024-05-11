using System.Collections.Generic;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT.Provider;

public class INFTCollectionProviderTests : NFTMarketServerApplicationTestBase
{
    private readonly INFTCollectionProvider _nftCollectionProvider;
    public INFTCollectionProviderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftCollectionProvider = GetRequiredService<INFTCollectionProvider>();
    }
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildMockIGraphQLHelper());
    }

    [Fact]
    public async void GetNFTCollectionsIndexAsyncTest()
    {
        long skipCount = 0;
        long maxResultCount = 1;
        string inputAddress = "aa";
        var addressList = new List<string> { inputAddress };
        var result= await _nftCollectionProvider.GetNFTCollectionsIndexAsync(skipCount, maxResultCount, addressList);
        result.ShouldNotBeNull();
        result.TotalRecordCount.ShouldBe(153);
        result.IndexerNftCollections.ShouldNotBeNull();
        result.IndexerNftCollections[0].Id.ShouldBe("tDVW-EEYUVVDFMR-0");
        result.IndexerNftCollections[0].Symbol.ShouldBe("EEYUVVDFMR-0");
    }
    
    [Fact]
    public async void GetNFTCollectionIndexAsyncTest()
    {
        string inputId = "tDVW-EEYUVVDFMR-0";
        var result = _nftCollectionProvider.GetNFTCollectionIndexAsync(inputId);
        result.Result.ShouldNotBeNull();
        result.Result.Id.ShouldBe("tDVW-EEYUVVDFMR-0");
        result.Result.Symbol.ShouldBe("EEYUVVDFMR-0");
    }
    [Fact]
    public async void GetNFTCollectionIndexByIdsAsyncTest()
    {
        var inputIds = new List<string>()
        {
            "tDVW-EEYUVVDFMR-0","tDVW-QNYQDHPPKU-0"
        };
        var result = _nftCollectionProvider.GetNFTCollectionIndexByIdsAsync(inputIds);
        result.Result["tDVW-EEYUVVDFMR-0"]
            .Id.ShouldBe("tDVW-EEYUVVDFMR-0");
        result.Result["tDVW-EEYUVVDFMR-0"]
            .Symbol.ShouldBe("EEYUVVDFMR-0");
    }
    
    private static IGraphQLHelper BuildMockIGraphQLHelper()
    {
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();

        var result2 = "{\"Data\":{\"Id\":\"tDVW-EEYUVVDFMR-0\",\"ChainId\":\"tDVW\",\"Symbol\":\"EEYUVVDFMR-0\",\"TokenName\":\"Description1\",\"TotalSupply\":1,\"IsBurnable\":true,\"IssueChainId\":1931928,\"ProxyOwnerAddress\":\"2s4kfNLykJiuDACgse9Ck6bTTDfk5XfXu78AurutKJLBKrdxzN\",\"ProxyIssuerAddress\":\"n6fnEeysCD4LK5MHraeriJ6kWPqMnmzF3HZURfAZgjyZJiXqS\",\"CreatorAddress\":\"2djZpW7uhneMC9ncB9BMrttyjgeCTecgDV4VcFUx7Xq5KBzTGX\",\"IsOfficial\":false,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"9a5e37e68af7764f7c6b86fb56e57cb6\"},{\"Key\":\"__nft_file_url\",\"Value\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1691673126367-7666.png_300.png\"},{\"Key\":\"__nft_feature_hash\",\"Value\":\"\"},{\"Key\":\"__nft_payment_tokens\",\"Value\":\"ELF\"},{\"Key\":\"__nft_metadata\",\"Value\":\"[]\"}]}}";
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<IndexerCommonResult<IndexerNFTCollection>>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(JsonConvert.DeserializeObject<IndexerCommonResult<IndexerNFTCollection>>(result2));
        
        var result3 = "{\"TotalRecordCount\":0,\"Data\":{\"TotalRecordCount\":153,\"IndexerNftCollections\":[{\"Id\":\"tDVW-EEYUVVDFMR-0\",\"ChainId\":\"tDVW\",\"Symbol\":\"EEYUVVDFMR-0\",\"TokenName\":\"Description1\",\"TotalSupply\":1,\"IsBurnable\":true,\"IssueChainId\":1931928,\"ProxyOwnerAddress\":\"2s4kfNLykJiuDACgse9Ck6bTTDfk5XfXu78AurutKJLBKrdxzN\",\"ProxyIssuerAddress\":\"n6fnEeysCD4LK5MHraeriJ6kWPqMnmzF3HZURfAZgjyZJiXqS\",\"CreatorAddress\":\"2djZpW7uhneMC9ncB9BMrttyjgeCTecgDV4VcFUx7Xq5KBzTGX\",\"IsOfficial\":false,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"9a5e37e68af7764f7c6b86fb56e57cb6\"},{\"Key\":\"__nft_file_url\",\"Value\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1691673126367-7666.png_300.png\"},{\"Key\":\"__nft_feature_hash\",\"Value\":\"\"},{\"Key\":\"__nft_payment_tokens\",\"Value\":\"ELF\"},{\"Key\":\"__nft_metadata\",\"Value\":\"[]\"}]},{\"Id\":\"tDVW-QNYQDHPPKU-0\",\"ChainId\":\"tDVW\",\"Symbol\":\"QNYQDHPPKU-0\",\"TokenName\":\"ADD\",\"TotalSupply\":1,\"IsBurnable\":true,\"IssueChainId\":1931928,\"ProxyOwnerAddress\":\"238TLXUppSKdVxwcV4nUZTpRCfFebZUrVnLWYX8P2qMjNVzPoX\",\"ProxyIssuerAddress\":\"jgjBQZMB9jrn5QhAmsgufV7776vRbtPB7vJThikp8DvbXPtU2\",\"CreatorAddress\":\"2djZpW7uhneMC9ncB9BMrttyjgeCTecgDV4VcFUx7Xq5KBzTGX\",\"IsOfficial\":false,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"1fd01cbeeb47cd1368103abf8b782a18\"},{\"Key\":\"__nft_file_url\",\"Value\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1691663108331-260x260.jpeg\"},{\"Key\":\"__nft_feature_hash\",\"Value\":\"\"},{\"Key\":\"__nft_payment_tokens\",\"Value\":\"ELF\"},{\"Key\":\"__nft_metadata\",\"Value\":\"[]\"}]}]}}";
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<IndexerNFTCollections>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(JsonConvert.DeserializeObject<IndexerNFTCollections>(result3));
        
        return mockIGraphQLHelper.Object;
    }
    
}