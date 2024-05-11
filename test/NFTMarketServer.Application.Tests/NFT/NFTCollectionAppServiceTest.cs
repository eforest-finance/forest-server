using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Grains;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Options;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT;

public sealed class NFTCollectionAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly NFTCollectionAppService _nftCollectionAppService;
    private readonly INESTRepository<NFTCollectionExtensionIndex, string> _nftCollectionExtensionRepository;

    public NFTCollectionAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftCollectionAppService = GetRequiredService<NFTCollectionAppService>();
        _nftCollectionExtensionRepository = GetRequiredService<INESTRepository<NFTCollectionExtensionIndex, string>>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildMockINFTCollectionProvider());
        services.AddSingleton(BuildINFTCollectionExtensionProvider());
        services.AddSingleton(MockRecommendedCollectionsOptions());
    }

    [Fact]
    public async Task CreateCollectionExtensionAsyncTest()
    {
        var input = new CreateCollectionExtensionInput
        {
            ChainId = "AELF",
            Symbol = "QWE-0",
            Description = "Description",
            TransactionId = "1a2b3c4567",
            ExternalLink = "http://www.test.com/test.link",
            LogoImage = "http://www.test.com/testLogo.jpg",
            FeaturedImage = "http://www.test.com/testFeatured.jpg"
        };
        await _nftCollectionAppService.CreateCollectionExtensionAsync(input);
        var id = GrainIdHelper.GenerateGrainId(input.ChainId, input.Symbol);
        var nftCollectionExtensionIndex = await _nftCollectionExtensionRepository.GetAsync(id);
        nftCollectionExtensionIndex.ChainId.ShouldBe(input.ChainId);
        nftCollectionExtensionIndex.NFTSymbol.ShouldBe(input.Symbol);
        nftCollectionExtensionIndex.TransactionId.ShouldBe(input.TransactionId);
        nftCollectionExtensionIndex.LogoImage.ShouldBe(input.LogoImage);
        nftCollectionExtensionIndex.Description.ShouldBe(input.Description);
        nftCollectionExtensionIndex.ExternalLink.ShouldBe(input.ExternalLink);
    }

    [Fact]
    public async Task GetNFTCollectionAsync_ShouldBe_NotNull()
    {
        string id = "aaa";
        var res = _nftCollectionAppService.GetNFTCollectionAsync(id);
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        //TestOutputHelper.WriteLine(JsonConvert.SerializeObject(res, settings)); 
        res.Result.Id.ShouldBe("tDVW-TESTR-0");
    }

    [Fact]
    public async Task GetNFTCollectionAsync_ShouldBe_Null()
    {
        string id = "bbb";
        var res = _nftCollectionAppService.GetNFTCollectionAsync(id);
        TestOutputHelper.WriteLine(JsonConvert.SerializeObject(res));
        res.Result.ShouldBeNull();
    }

    [Fact]
    public async Task GetNFTCollectionsAsync_ShouldBe26()
    {
        var res = _nftCollectionAppService.GetNFTCollectionsAsync(
            new GetNFTCollectionsInput
            {
                SkipCount = 0,
                MaxResultCount = 2,
                Address = ""
            });
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        //TestOutputHelper.WriteLine(JsonConvert.SerializeObject(res, settings));
        res.Result.TotalCount.ShouldBe(26);
    }

    [Fact]
    public async Task GetNFTCollectionsAsync_ShouldBe0()
    {
        var res = _nftCollectionAppService.GetNFTCollectionsAsync(
            new GetNFTCollectionsInput
            {
                SkipCount = 0,
                MaxResultCount = 2,
                Address = "aaa"
            });
        TestOutputHelper.WriteLine(JsonConvert.SerializeObject(res));
        res.Result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task TestSearchNFTCollectionsAsync()
    {
        var input = new SearchNFTCollectionsInput()
        {
            TokenName = "AAA",
            Sort = "Name",
            SortType = SortOrder.Descending
        };
        var result = await _nftCollectionAppService.SearchNFTCollectionsAsync(input);
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(1);
        result.Items[0].Symbol.ShouldBe("LIUGEKKKKKK-0");
    }

    [Fact]
    public async Task TestGetRecommendedNFTCollectionsAsync()
    {
        var result = await _nftCollectionAppService.GetRecommendedNFTCollectionsAsync();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Symbol.ShouldBe("LIUGEKKKKKK-0");
    }

    private static INFTCollectionExtensionProvider BuildINFTCollectionExtensionProvider()
    {
        var result1 = "{\"Item1\":1,\"Item2\":[{\"Id\":\"tDVV-LIUGEKKKKKK-0\",\"ChainId\":\"tDVV\",\"NFTSymbol\":\"LIUGEKKKKKK-0\",\"LogoImage\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1700570966368-230901_003.jpg\",\"FeaturedImage\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1700570982244-20231019006.jpeg\",\"Description\":\"wulalala\",\"TransactionId\":\"1b2bb8f2bcd8890d924a8dc0441477dc75f59e1ed2fb07eed0ab2c2a6dcd94af\",\"ExternalLink\":\"http://www.baidu.com\",\"ItemTotal\":1,\"OwnerTotal\":2,\"FloorPrice\":1,\"FloorPriceSymbol\":\"ELF\",\"TokenName\":\"LIUGEKKKKKK\",\"CreateTime\":\"2023-11-21T12:50:14Z\"}]}";
        
        var mockProvider = new Mock<INFTCollectionExtensionProvider>();
        mockProvider.Setup(calc =>
            calc.GetNFTCollectionExtensionAsync(It.IsAny<SearchNFTCollectionsInput>())).ReturnsAsync(
            JsonConvert.DeserializeObject<Tuple<long, List<NFTCollectionExtensionIndex>>>(result1));

        return mockProvider.Object;
    }

    private static IOptionsMonitor<RecommendedCollectionsOptions> MockRecommendedCollectionsOptions()
    {
        var optionsMonitorMock = new Mock<IOptionsMonitor<RecommendedCollectionsOptions>>();
        
        var recommendedCollectionsOptions = new RecommendedCollectionsOptions
        {
            RecommendedCollections = new List<RecommendedCollection>()
            {
                new RecommendedCollection()
                {
                    id = "tDVV-LIUGEKKKKKK-0"
                }
            }
        };
        
        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(recommendedCollectionsOptions);
        return optionsMonitorMock.Object;
    }

    private static INFTCollectionProvider BuildMockINFTCollectionProvider()
    {
        var result =
            "{\"Id\":\"tDVW-TESTR-0\",\"ChainId\":\"tDVW\",\"Symbol\":\"TESTR-0\",\"TokenName\":\"lstest222\",\"TotalSupply\":1,\"IsBurnable\":true,\"IssueChainId\":1931928,\"Creator\":null,\"CreatorAddress\":\"28TXjL2Zq3YEQsyZzW9DESer1Hcydzssh5ZQwqmuRDsRjQwDAo\",\"LogoImage\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1689315734714-_%E5%9B%BE%E5%B1%82_1.png\",\"FeaturedImage\":\"\",\"Description\":\"\",\"IsOfficial\":false,\"BaseUrl\":null,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"\\\"88902e066e81ff0ca44b3867351acb6d\\\"\",\"Data\":null},{\"Key\":\"__nft_feature_hash\",\"Value\":\"\",\"Data\":null},{\"Key\":\"__nft_payment_tokens\",\"Value\":\"ELF\",\"Data\":null},{\"Key\":\"__nft_metadata\",\"Value\":\"[]\",\"Data\":null}],\"Data\":null}";

        var mockINFTCollectionProvider = new Mock<INFTCollectionProvider>();
        mockINFTCollectionProvider.Setup(calc =>
            calc.GetNFTCollectionIndexAsync("aaa")).ReturnsAsync(
            JsonConvert.DeserializeObject<IndexerNFTCollection>(result));

        var result2 =
            "{\"TotalRecordCount\":26,\"IndexerNftCollections\":[{\"Id\":\"tDVW-XXPFQPSFZF-0\",\"ChainId\":\"tDVW\",\"Symbol\":\"XXPFQPSFZF-0\",\"TokenName\":\"XXPFQPSFZF-0token\",\"TotalSupply\":10000,\"IsBurnable\":true,\"IssueChainId\":1931928,\"Creator\":null,\"CreatorAddress\":\"2L4XSrSrFBMap1phcXNG1Eg8YZjMwAzqWDgJoSTcLDjcdmRJW5\",\"LogoImage\":null,\"FeaturedImage\":null,\"Description\":null,\"IsOfficial\":false,\"BaseUrl\":null,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_image_url\",\"Value\":\"https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/XXPFQPSFZF-0.jpg\",\"Data\":null}],\"Data\":null},{\"Id\":\"tDVW-TESTH-0\",\"ChainId\":\"tDVW\",\"Symbol\":\"TESTH-0\",\"TokenName\":\"LSTEST\",\"TotalSupply\":1,\"IsBurnable\":true,\"IssueChainId\":1931928,\"Creator\":null,\"CreatorAddress\":\"2vrQb6mt1ToURDZcrEDd3EaX2mKKhgeAr4MZpcLeGXR2MZSyBX\",\"LogoImage\":null,\"FeaturedImage\":null,\"Description\":null,\"IsOfficial\":false,\"BaseUrl\":null,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"\\\"0e0a3ed9d56bd196764de35b966d425a\\\"\",\"Data\":null},{\"Key\":\"__nft_feature_hash\",\"Value\":\"\",\"Data\":null},{\"Key\":\"__nft_payment_tokens\",\"Value\":\"ELF\",\"Data\":null},{\"Key\":\"__nft_metadata\",\"Value\":\"[]\",\"Data\":null}],\"Data\":null}],\"Data\":null}";
        mockINFTCollectionProvider.Setup(calc =>
            calc.GetNFTCollectionsIndexAsync(0, 2, new List<string>{})).ReturnsAsync(
            JsonConvert.DeserializeObject<IndexerNFTCollections>(result2));
        
        var result3 = "{\"tDVV-LIUGEKKKKKK-0\":{\"Id\":\"tDVV-LIUGEKKKKKK-0\",\"ChainId\":\"tDVV\",\"Symbol\":\"LIUGEKKKKKK-0\",\"TokenName\":\"LIUGEKKKKKK\",\"TotalSupply\":1,\"IsBurnable\":true,\"IssueChainId\":1866392,\"ProxyOwnerAddress\":\"Ke8opsEQxSB9JMCZHoQe4r531rUoRvyW3PVBtaSPMXaAbibZR\",\"ProxyIssuerAddress\":\"22EPS2zn8Nj5hYj4yVWSyiPMpDqy9wAp36nhc6TMXZ25pvxKqw\",\"CreatorAddress\":\"jTN2xssginjkyTP3BEFdJVrP64piDDiH4EqXGx53hFJTw6QnK\",\"LogoImage\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1700570966368-230901_003.jpg\",\"FeaturedImage\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1700570982244-20231019006.jpeg\",\"Description\":\"wulalala\",\"IsOfficial\":false,\"BaseUrl\":\"http://www.baidu.com\",\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"5ba9ce6cde03143d65bf187b787fe6cb\"},{\"Key\":\"__nft_feature_hash\",\"Value\":\"d8663f33b58d12829759cf7bcddcef44\"},{\"Key\":\"__nft_payment_tokens\",\"Value\":\"ELF\"},{\"Key\":\"__nft_metadata\",\"Value\":\"[{\\\\\\\"key\\\\\\\":\\\\\\\"description1\\\\\\\",\\\\\\\"value\\\\\\\":\\\\\\\"aaaa\\\\\\\"}]\"},{\"Key\":\"__nft_image_url\",\"Value\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1700570966368-230901_003.jpg\"}],\"BlockHeight\":0,\"CreateTime\":\"2023-11-21T12:50:14Z\"}}";
        mockINFTCollectionProvider.Setup(calc =>
            calc.GetNFTCollectionIndexByIdsAsync(It.IsAny<List<string>>())).ReturnsAsync(
            JsonConvert.DeserializeObject<Dictionary<string, IndexerNFTCollection>>(result3));
        return mockINFTCollectionProvider.Object;
    }
}