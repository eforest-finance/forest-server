using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Grains;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Dto;
using NFTMarketServer.Users.Index;
using NFTMarketServer.Users.Provider;
using NSubstitute;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Threading;
using Volo.Abp.Users;
using NFTMarketServer.Seed.Index;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT;

public sealed partial class NftInfoAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly INESTRepository<NFTInfoExtensionIndex, string> _nftInfoExtensionIndexRepository;
    private ICurrentUser _currentUser;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IUserAppService _userAppService;
    private readonly INESTRepository<UserIndex, Guid> _userIndexRepository;

    public NftInfoAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftInfoAppService = GetRequiredService<INFTInfoAppService>();
        _nftInfoExtensionIndexRepository = GetRequiredService<INESTRepository<NFTInfoExtensionIndex, string>>();
        _userInformationProvider = GetRequiredService<IUserInformationProvider>();
        _userAppService = GetRequiredService<IUserAppService>();
        _userIndexRepository = GetRequiredService<INESTRepository<UserIndex, Guid>>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockTokenAppService());
        services.AddSingleton(BuildMockINFTInfoProvider());
        services.AddSingleton(MockListingWhiteListPriceProvider());
        services.AddSingleton(MockISeedInfoProvider());
        services.AddSingleton(MockNFTInfoSyncedProviderProvider());
        services.AddSingleton(MockNFTCollectionExtensionProvider());
        services.AddSingleton(MockNFTDealInfoProvider());
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(MockNFTListingProvider());
        services.AddSingleton(MockNFTOfferProvider());
        services.AddSingleton(MockISeedSymbolSyncedProvider());
        services.AddSingleton(MockINFTInfoSyncedProvider());
        services.AddSingleton(MockINFTListingProvider());
        services.AddSingleton(MockIInscriptionProvider());
    }

    [Fact]
    public async void CreateNFTInfoExtensionAsyncTest()
    {
        var input = new CreateNFTExtensionInput
        {
            ChainId = "AELF",
            Symbol = "QWE-1",
            TransactionId = "1a2b3c4567",
            PreviewImage = "http://www.test.com/detail/gO6Qk.html",
            File = "http://www.test.com/test.jpg",
            Description = "Description",
            ExternalLink = "http://www.test.com/test.link"
        };
        await _nftInfoAppService.CreateNFTInfoExtensionAsync(input);
        var id = GrainIdHelper.GenerateGrainId(input.ChainId, input.Symbol);
        var nftInfoExtensionIndex = await _nftInfoExtensionIndexRepository.GetAsync(id);
        nftInfoExtensionIndex.ChainId.ShouldBe(input.ChainId);
        nftInfoExtensionIndex.NFTSymbol.ShouldBe(input.Symbol);
        nftInfoExtensionIndex.TransactionId.ShouldBe(input.TransactionId);
        nftInfoExtensionIndex.PreviewImage.ShouldBe(input.PreviewImage);
        nftInfoExtensionIndex.File.ShouldBe(input.File);
        nftInfoExtensionIndex.Description.ShouldBe(input.Description);
        nftInfoExtensionIndex.ExternalLink.ShouldBe(input.ExternalLink);
    }

    [Fact]
    public async Task GetNFTInfoAsync_ShouldBe_NotNull()
    {
        var input = new GetNFTInfoInput
        {
            Id = "tDVV-LIJIGUANGAAAABBB-1",
            Address = "T7ApxUrF6vYfBizHBLSrfiEgEEZH2yURp3stye5AJLyc2F96z"
        };
        var res = await _nftInfoAppService.GetNFTInfoAsync(input);

        res.Id.ShouldBe("tDVV-LIJIGUANGAAAABBB-1");
        res.OwnerCount.ShouldBe(1);
    }

    [Fact]
    public async Task TestGetNFTInfosForUesrProfileAsync()
    {
        var input = new GetNFTInfosProfileInput
        {
            NFTCollectionId = "AAA-0",
            SkipCount = 0,
            MaxResultCount = 2,
            Address = "",
            Status = 2,
            IssueAddress = ""
        };
        var res = await _nftInfoAppService.GetNFTInfosForUserProfileAsync(input);
        res.TotalCount.ShouldBe(1);

    }
    
    [Fact]
    public async Task TestGetCompositeNFTInfosAsync()
    {
        var input = new GetCompositeNFTInfosInput
        {
            ChainList = new List<string> { "tDVV" },
            CollectionType = "nft",
            CollectionId = "tDVV-LIUGEKKKKKK-0",
            SkipCount = 0,
            MaxResultCount = 1,
            Sorting = "Price High to Low"
        };
        var res = await _nftInfoAppService.GetCompositeNFTInfosAsync(input);
        res.TotalCount.ShouldBe(1);
        res.Items[0].ChainIdStr.ShouldBe("tDVV");
    }
    
    
    [Fact]
    public async Task TestGetCompositeNFTInfosAsyncSeed()
    {
        var input = new GetCompositeNFTInfosInput
        {
            ChainList = new List<string> { "tDVV" },
            CollectionType = "seed",
            CollectionId = "tDVV-SEED-0",
            SkipCount = 0,
            MaxResultCount = 1,
            Sorting = "Price High to Low"
        };
        var res = await _nftInfoAppService.GetCompositeNFTInfosAsync(input);
        res.TotalCount.ShouldBe(1);
        res.Items[0].Id.ShouldBe("tDVV-SEED-666666");
    }
    
    [Fact]
    public async Task TestGetNFTForSaleWithLogin()
    {
        Login();
        var input = new GetNFTForSaleInput
        {
            Id = "tDVV-LIJIGUANGAAAABBB-1"
        };
        var res = await _nftInfoAppService.GetNFTForSaleAsync(input);
        res.ShouldNotBeNull();
        res.AvailableQuantity.ShouldBe(3);
        res.MaxOfferPrice.ShouldBe(2);
    }
    
    [Fact]
    public async Task TestGetNFTForSaleWithOutLogin()
    {
        var input = new GetNFTForSaleInput
        {
            Id = "tDVV-LIJIGUANGAAAABBB-1",
            
        };
        var res = await _nftInfoAppService.GetNFTForSaleAsync(input);
        res.ShouldNotBeNull();
        res.AvailableQuantity.ShouldBe(6);
        res.MaxOfferPrice.ShouldBe(2);
    }
    
    [Fact]
    public async Task TestGetNFTOwnersAsync()
    {
        var input = new GetNFTOwnersInput
        {
            Id = "tDVV-LIJIGUANGAAAABBB-1",
            ChainId = "tdVV"
        };
        var res = await _nftInfoAppService.GetNFTOwnersAsync(input);
        res.ShouldNotBeNull();
        res.TotalCount.ShouldBe(1);
        res.Items.Count.ShouldBe(1);
    }
    
    private static INFTInfoProvider BuildMockINFTInfoProvider()
    {
        var result =
            "{\"Id\":\"tDVW-JINMINGTRT-1\",\"ChainId\":\"tDVW\",\"IssueChainId\":1931928,\"Symbol\":\"JINMINGTRT-1\",\"Issuer\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Owner\":null,\"OwnerCount\":1,\"Issued\":0,\"TokenName\":\"amberN1\",\"TotalSupply\":1,\"WhitelistId\":\"26163e47d4ec49b4b41ece0ada05955522340cf9dcc38a6723fe4dcf8209c069\",\"CreatorAddress\":null,\"ImageUrl\":null,\"CollectionSymbol\":null,\"CollectionName\":null,\"CollectionId\":\"tDVW-JINMINGTRT-0\",\"ListingId\":null,\"ListingAddress\":null,\"ListingPrice\":0.0,\"ListingQuantity\":0,\"ListingEndTime\":null,\"LatestListingTime\":null,\"LatestDealPrice\":0.0,\"LatestDealTime\":\"0001-01-01T00:00:00\",\"ListingToken\":null,\"LatestDealToken\":null,\"PreviewImage\":null,\"File\":null,\"FileExtension\":null,\"Description\":null,\"IsOfficial\":false,\"Category\":0,\"ExternalInfoDictionary\":null,\"Data\":null}";

        var mockINFTInfoProvider = new Mock<INFTInfoProvider>();
        mockINFTInfoProvider.Setup(calc =>
            calc.GetNFTSupplyAsync("tDVW-JINMINGTRT-1")).ReturnsAsync(
            JsonConvert.DeserializeObject<IndexerNFTInfo>(result));
        mockINFTInfoProvider.Setup(calc =>
            calc.GetNFTInfoIndexAsync("tDVW-JINMINGTRT-1", It.IsAny<string>())).ReturnsAsync(
            JsonConvert.DeserializeObject<IndexerNFTInfo>(result));

        var result2 =
            "{\"TotalRecordCount\":5,\"IndexerNftInfos\":[{\"Id\":\"tDVW-JINMINGTRTT-5622\",\"ChainId\":\"tDVW\",\"IssueChainId\":1931928,\"Symbol\":\"JINMINGTRTT-5622\",\"Issuer\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Owner\":null,\"OwnerCount\":2,\"Issued\":0,\"TokenName\":\"amberN5622\",\"TotalSupply\":1000,\"WhitelistId\":null,\"CreatorAddress\":null,\"ImageUrl\":null,\"CollectionSymbol\":null,\"CollectionName\":null,\"CollectionId\":\"tDVW-JINMINGTRTT-0\",\"ListingId\":\"tDVW-JINMINGTRTT-5622-2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV-1689242806\",\"ListingAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"ListingPrice\":10.0,\"ListingQuantity\":69,\"ListingEndTime\":\"2024-01-12T10:06:46.6548821Z\",\"LatestListingTime\":\"2023-07-13T10:43:02.0917935Z\",\"LatestDealPrice\":10.0,\"LatestDealTime\":\"2023-07-13T10:43:02.0917935Z\",\"ListingToken\":{\"ChainId\":\"tDVW\",\"Address\":\"cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp\",\"Symbol\":\"ELF\",\"Decimals\":\"8\",\"Id\":\"tDVW-ELF\"},\"LatestDealToken\":{\"ChainId\":\"tDVW\",\"Address\":\"\\\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\\\"\",\"Symbol\":\"JINMINGTRTT-5622\",\"Decimals\":\"0\",\"Id\":\"tDVW-JINMINGTRTT-5622\"},\"PreviewImage\":null,\"File\":null,\"FileExtension\":null,\"Description\":null,\"IsOfficial\":false,\"Category\":0,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"\\\"9fd7abe078427234f793f4cdac834874\\\"\",\"Data\":null}],\"Data\":null},{\"Id\":\"tDVW-JINMINGTRTT-123\",\"ChainId\":\"tDVW\",\"IssueChainId\":1931928,\"Symbol\":\"JINMINGTRTT-123\",\"Issuer\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"Owner\":null,\"Issued\":0,\"TokenName\":\"amberN123\",\"TotalSupply\":1000,\"WhitelistId\":null,\"CreatorAddress\":null,\"ImageUrl\":null,\"CollectionSymbol\":null,\"CollectionName\":null,\"CollectionId\":\"tDVW-JINMINGTRTT-0\",\"ListingId\":\"tDVW-JINMINGTRTT-123-2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV-1689330350\",\"ListingAddress\":\"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV\",\"ListingPrice\":122.0,\"ListingQuantity\":10,\"ListingEndTime\":\"2024-01-13T10:25:50.0599602Z\",\"LatestListingTime\":\"2023-07-14T10:25:50.0599602Z\",\"LatestDealPrice\":0.0,\"LatestDealTime\":\"0001-01-01T00:00:00\",\"ListingToken\":{\"ChainId\":\"tDVW\",\"Address\":\"cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp\",\"Symbol\":\"ELF\",\"Decimals\":\"8\",\"Id\":\"tDVW-ELF\"},\"LatestDealToken\":null,\"PreviewImage\":null,\"File\":null,\"FileExtension\":null,\"Description\":null,\"IsOfficial\":false,\"Category\":0,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"\\\"378c10a56a7302ccba964098b331c7c5\\\"\",\"Data\":null}],\"Data\":null}],\"Data\":null}";
        mockINFTInfoProvider.Setup(calc =>
                calc.GetNFTInfoIndexsAsync(0, 2, "tDVW-JINMINGTRTT-0", "ListingTime ASC", 1, 0, 0, "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp", "", null))
            .ReturnsAsync(
                JsonConvert.DeserializeObject<IndexerNFTInfos>(result2));

        var result3 = "{\"TotalRecordCount\":1,\"IndexerNftInfos\":[{\"Id\":\"tDVV-LIUGEKKKKKK-666666\",\"ChainId\":\"tDVV\",\"IssueChainId\":1866392,\"Symbol\":\"LIUGEKKKKKK-666666\",\"Issuer\":\"HFJAYSsMYY9D8N558i8vffDeLrQw7zyfMFWH8rtMatD265EHq\",\"ProxyIssuerAddress\":\"22EPS2zn8Nj5hYj4yVWSyiPMpDqy9wAp36nhc6TMXZ25pvxKqw\",\"OwnerCount\":0,\"Issued\":0,\"TokenName\":\"LIUGEKKKKKKITEM\",\"TotalSupply\":100,\"ImageUrl\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1700571666041-20231019008.jpeg\",\"CollectionId\":\"tDVV-LIUGEKKKKKK-0\",\"OtherOwnerListingFlag\":false,\"ListingPrice\":0,\"ListingQuantity\":0,\"ListingEndTime\":\"0001-01-01T00:00:00\",\"LatestListingTime\":\"2023-11-24T06:20:01.7924183Z\",\"LatestDealPrice\":1,\"LatestDealTime\":\"2023-11-24T06:20:01.7924183Z\",\"LatestDealToken\":{\"ChainId\":\"tDVV\",\"Address\":\"22EPS2zn8Nj5hYj4yVWSyiPMpDqy9wAp36nhc6TMXZ25pvxKqw\",\"Symbol\":\"LIUGEKKKKKK-666666\",\"Decimals\":\"0\",\"Id\":\"tDVV-LIUGEKKKKKK-666666\"},\"IsOfficial\":false,\"ExternalInfoDictionary\":[{\"Key\":\"__nft_file_hash\",\"Value\":\"ae32fac6e377e589b13855c9a5d976ba\"},{\"Key\":\"__nft_metadata\",\"Value\":\"[{\\\\\\\"key\\\\\\\":\\\\\\\"description1\\\\\\\",\\\\\\\"value\\\\\\\":\\\\\\\"bbbbb\\\\\\\"}]\"},{\"Key\":\"__nft_fileType\",\"Value\":\"image\"},{\"Key\":\"__nft_image_url\",\"Value\":\"https://forest-dev.s3.ap-northeast-1.amazonaws.com/1700571666041-20231019008.jpeg\"}]}]}";

        mockINFTInfoProvider.Setup(calc =>
                calc.GetNFTInfoIndexsUserProfileAsync(It.IsAny<GetNFTInfosProfileInput>()))
            .ReturnsAsync(
                JsonConvert.DeserializeObject<IndexerNFTInfos>(result3));

        var result4 = "{\n  \"TotalRecordCount\": 1,\n  \"IndexerNFTBriefInfoList\": [\n    {\n      \"CollectionSymbol\": \"LIUGEKKKKKK-0\",\n      \"NFTSymbol\": \"LIUGEKKKKKK-666666\",\n      \"PriceDescription\": \"Price\",\n      \"Price\": 1.0,\n      \"Id\": \"tDVV-LIUGEKKKKKK-666666\",\n      \"TokenName\": \"LIUGEKKKKKKITEM\",\n      \"IssueChainIdStr\": \"tDVV\",\n      \"ChainIdStr\": \"tDVV\"\n    }\n  ]\n}";
        mockINFTInfoProvider.Setup(calc =>
                calc.GetNFTBriefInfosAsync(It.IsAny<GetCompositeNFTInfosInput>()))
            .ReturnsAsync(
                JsonConvert.DeserializeObject<IndexerNFTBriefInfos>(result4));
        
        
        var userBalance = new IndexerNFTUserBalance()
        {
            Id = "dd5a09f0-3de0-4e49-95e9-9be08dd5bf22",
            Address = "2jh7mbhQJZTdxrQjHS8NAAyqKvQ2T84XZpzu17BMAYfpNRTV3n",
            Amount = 1
        };
        var ownersInfo = new IndexerNFTOwners
        {
            TotalCount = 1,
            IndexerNftUserBalances = new List<IndexerNFTUserBalance> { userBalance }
        };
        mockINFTInfoProvider.Setup(calc =>
                calc.GetNFTOwnersAsync(It.Is<GetNFTOwnersInput>(input => input.Id == "tDVV-LIJIGUANGAAAABBB-1")))
            .ReturnsAsync(ownersInfo);
        
        return mockINFTInfoProvider.Object;
    }

    private static ISeedInfoProvider MockISeedInfoProvider()
    {
        var mockISeedInfoProvider = new Mock<ISeedInfoProvider>();
        var result1 = "{\n  \"TotalRecordCount\": 28,\n  \"IndexerSeedBriefInfoList\": [\n    {\n      \"CollectionSymbol\": \"SEED-0\",\n      \"NFTSymbol\": \"DBUNXX-0\",\n      \"PreviewImage\": \"https://forest-dev.s3.amazonaws.com/SymbolMarket-test4/SEED-2041.svg\",\n      \"PriceDescription\": \"\",\n      \"Price\": -1.0,\n      \"Id\": \"tDVV-SEED-2041\",\n      \"TokenName\": \"SEED-DBUNXX-0\",\n      \"IssueChainIdStr\": \"AELF\",\n      \"ChainIdStr\": \"AELF\"\n    }\n  ]\n}";
        mockISeedInfoProvider.Setup(calc =>
                calc.GetSeedBriefInfosAsync(It.IsAny<GetCompositeNFTInfosInput>()))
            .ReturnsAsync(
                JsonConvert.DeserializeObject<IndexerSeedBriefInfos>(result1));
        
        var result2 = "{\"TotalRecordCount\":0,\"IndexerSeedInfoList\":[]}";

        mockISeedInfoProvider.Setup(calc =>
                calc.GetSeedInfosUserProfileAsync(It.IsAny<GetNFTInfosProfileInput>()))
            .ReturnsAsync(
                JsonConvert.DeserializeObject<IndexerSeedInfos>(result2));

        return mockISeedInfoProvider.Object;
    }

    private static INFTListingWhitelistPriceProvider MockListingWhiteListPriceProvider()
    {
        var result =
            "[{\"listingId\":\"tDVW-JINMINGTRT-1-4FHi2nS1MkmJL7N9WHPsNEjnSVqGgwghszfC6JMXy2KL7LNcv-1692089694\",\"quantity\":1,\"startTime\":\"2023-08-15T08:54:54.150364Z\",\"publicTime\":\"2023-08-15T08:54:54.150364Z\",\"expireTime\":\"2023-08-15T09:54:54.150364Z\",\"durationHours\":1,\"offerFrom\":null,\"nftInfoId\":\"tDVW-JINMINGTRT-1\",\"owner\":\"4FHi2nS1MkmJL7N9WHPsNEjnSVqGgwghszfC6JMXy2KL7LNcv\",\"prices\":1,\"whiteListPrice\":null,\"whitelistId\":null,\"whitelistPriceToken\":{\"id\":\"tDVW-ELF\",\"chainId\":\"tDVW\",\"symbol\":\"ELF\",\"decimals\":8}}]";
        var priceProvider = new Mock<INFTListingWhitelistPriceProvider>();
        priceProvider.Setup(calc => 
            calc.GetNFTListingWhitelistPricesAsync(
                It.IsAny<string>(), 
                It.Is<List<string>>(list => list.Contains("tDVW-JINMINGTRT-1"))))
            .ReturnsAsync(JsonConvert.DeserializeObject<List<IndexerListingWhitelistPrice>>(result));
        return priceProvider.Object;
    }

    private static INFTInfoSyncedProvider MockNFTInfoSyncedProviderProvider()
    {
        var result =
            "{\"id\":\"tDVV-LIJIGUANGAAAABBB-1\",\"symbol\":\"LIJIGUANGAAAABBB-1\",\"tokenName\":\"721\", \"collectionSymbol\":\"LIJIGUANGAAAABBB-0\", \"chainId\":\"tDVV\", \"totalSupply\": 1, \"collectionId\": \"tDVV-LIJIGUANGAAAABBB-0\", \"OwnerCount\": 1, \"issuer\": \"aaaaa\", \"owner\": \"bbbbb\"}";

        var syncProvider = new Mock<INFTInfoSyncedProvider>();
        syncProvider.Setup(calc => 
                calc.GetNFTInfoIndexAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(JsonConvert.DeserializeObject<IndexerNFTInfo>(result));

        syncProvider.Setup(calc => 
                calc.GetNFTInfosUserProfileAsync(
                    It.IsAny<GetNFTInfosProfileInput>()))
            .ReturnsAsync(new IndexerNFTInfos
            {
                TotalRecordCount = 1,
                IndexerNftInfos = new List<IndexerNFTInfo>
                {
                    new IndexerNFTInfo
                    {
                        Id = "tDVV-LIJIGUANGAAAABBB-1",
                        ChainId = "tDVV",
                    }
                }
            });
        return syncProvider.Object;
    }
    
    private static INFTCollectionExtensionProvider MockNFTCollectionExtensionProvider()
    {
        var result =
            "{\"id\":\"tDVV-LIJIGUANGAAAABBB-1\",\"tokenName\":\"ELF\", \"chainId\":\"tDVV-0\"}";
        var provider = new Mock<INFTCollectionExtensionProvider>();
        provider.Setup(calc => 
                calc.GetNFTCollectionExtensionAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(JsonConvert.DeserializeObject<NFTCollectionExtensionIndex>(result));
        return provider.Object;
    }

    private static INFTDealInfoProvider MockNFTDealInfoProvider()
    {
        var provider = new Mock<INFTDealInfoProvider>();
        provider.Setup(calc => 
                calc.GetDealInfosAsync(
                    It.IsAny<GetNftDealInfoDto>()))
            .ReturnsAsync(new IndexerNFTDealInfos
            {
                Data = new IndexerNFTDealInfos
                {
                    TotalRecordCount = 1,
                    IndexerNftDealList = new List<IndexerNFTDealInfo>
                    {
                        new IndexerNFTDealInfo
                        {
                            PurchaseAmount = 13000000,
                            PurchaseSymbol = "ELF",
                        }
                    }
                }
            });
        return provider.Object;
    }
    
    private async Task Login()
    {
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Email.Returns("xxx@gmail.com");
        UserSourceInput userSourceInput = new UserSourceInput
        {
            UserId = userId,
            AelfAddress = "4FHi2nS1MkmJL7N9WHPsNEjnSVqGgwghszfC6JMXy2KL7LNcv"
        };
        AsyncHelper.RunSync(async () =>
        {
            await _userInformationProvider.SaveUserSourceAsync(userSourceInput);
        });
    }
    
    
    private INFTListingProvider MockNFTListingProvider()
    {
        var listing1 = new IndexerNFTListingInfo()
        {
            Symbol = "LIJIGUANGAAAABBB-1",
            Quantity = 1,
            Prices = 10,
            Owner = "T7ApxUrF6vYfBizHBLSrfiEgEEZH2yURp3stye5AJLyc2F96z",
            NftInfo = new IndexerNFTInfo
            {
                Symbol = "LIJIGUANGAAAABBB-1",
            }
        };
        
        var listing2 = new IndexerNFTListingInfo()
        {
            Symbol = "LIJIGUANGAAAABBB-1",
            Quantity = 2,
            Prices = 20,
            Owner = "2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV",
            NftInfo = new IndexerNFTInfo
            {
                Symbol = "LIJIGUANGAAAABBB-1",
            }
        };
        
        var listing3 = new IndexerNFTListingInfo()
        {
            Symbol = "LIJIGUANGAAAABBB-1",
            Quantity = 3,
            Prices = 20,
            Owner = "4FHi2nS1MkmJL7N9WHPsNEjnSVqGgwghszfC6JMXy2KL7LNcv",
            NftInfo = new IndexerNFTInfo
            {
                Symbol = "LIJIGUANGAAAABBB-1",
            }
        };
        
        var provider = new Mock<INFTListingProvider>();
        
        provider
            .Setup(provider => provider.GetNFTListingsAsync(It.Is<GetNFTListingsDto>(dto => dto.ExcludedAddress.IsNullOrEmpty()
               )))
            .ReturnsAsync(new PagedResultDto<IndexerNFTListingInfo>()
            {

                TotalCount = 0,
                Items = new List<IndexerNFTListingInfo> { listing1, listing2, listing3 }
            });
        
        provider
            .Setup(provider => provider.GetNFTListingsAsync(It.Is<GetNFTListingsDto>(dto => dto.ExcludedAddress == "4FHi2nS1MkmJL7N9WHPsNEjnSVqGgwghszfC6JMXy2KL7LNcv"
            )))
            .ReturnsAsync(new PagedResultDto<IndexerNFTListingInfo>()
            {
                TotalCount = 2,
                Items = new List<IndexerNFTListingInfo> { listing1, listing2 }
            });


        provider
            .Setup(provider => provider.GetNFTListingsAsync(It.Is<GetNFTListingsDto>(dto => dto.ExcludedAddress == "T7ApxUrF6vYfBizHBLSrfiEgEEZH2yURp3stye5AJLyc2F96z"
            )))
            .ReturnsAsync(new PagedResultDto<IndexerNFTListingInfo>()
            {
                TotalCount = 0
            });
        return provider.Object;
    }
    
    private static INFTOfferProvider MockNFTOfferProvider()
    {
        var provider = new Mock<INFTOfferProvider>();
        provider.Setup(calc => 
                calc.GetMaxOfferInfoAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(new IndexerNFTOffer
            {
                Id = "offerId",
                Price = 2,
                PurchaseToken = new IndexerNFTOfferPurchaseToken
                {
                    Symbol = "ELF"
                }
                
            });
        return provider.Object;
    }
    
    private static INFTInfoSyncedProvider MockINFTInfoSyncedProvider()
    {
        var mockNftInfoSyncedProvider = new Mock<INFTInfoSyncedProvider>();
        var nftList = new List<IndexerNFTInfo>();
        var nftInfoIndex = new IndexerNFTInfo()
        {
            Id = "tDVV-AAA-666666",
            CollectionId = "AAA-0",
            Symbol = "AAA-666666",
            ChainId = "tDVV"
        };
        nftList.Add(nftInfoIndex);
        var indexNftList = new List<IndexerNFTInfo>();
        var indexerNftInfo = new IndexerNFTInfo
        {
            Id = "tDVV-AAA-666666",
            CollectionId = "AAA-0",
            Symbol = "AAA-666666",
            ChainId = "tDVV"
        };
        indexNftList.Add(indexerNftInfo);
        var indexerNftInfos = new IndexerNFTInfos()
        {
            TotalRecordCount = indexNftList.Count,
            IndexerNftInfos = indexNftList
        };
        
        mockNftInfoSyncedProvider.Setup(calc =>
                calc.GetNFTInfoIndexAsync(It.IsAny<string>()))
            .ReturnsAsync(indexerNftInfo);
        
        mockNftInfoSyncedProvider.Setup(calc =>
                calc.GetNFTBriefInfosAsync(It.IsAny<GetCompositeNFTInfosInput>()))
            .ReturnsAsync(
                new Tuple<long, List<IndexerNFTInfo>>(nftList.Count, nftList));
         
        mockNftInfoSyncedProvider.Setup(calc =>
                calc.GetNFTInfosUserProfileAsync(It.IsAny<GetNFTInfosProfileInput>()))
            .ReturnsAsync(indexerNftInfos);
        return mockNftInfoSyncedProvider.Object;
    }
    
    private static ISeedSymbolSyncedProvider MockISeedSymbolSyncedProvider()
    {
        var mockSeedSymbolSyncedProvider = new Mock<ISeedSymbolSyncedProvider>();
        var seedsList = new List<SeedSymbolIndex>();
        var seedInfoIndex = new SeedSymbolIndex
        {
            Id = "tDVV-SEED-666666",
            Symbol = "SEED-666666",
            ChainId = "tDVV"
        };
        seedsList.Add(seedInfoIndex);
        var indexSeedList = new List<IndexerSeedInfo>();
        var indexerSeedInfo = new IndexerSeedInfo
        { 
            Id = "tDVV-SEED-666666",
            Symbol = "SEED-666666",
            ChainId = "tDVV"
        };
        indexSeedList.Add(indexerSeedInfo);
        var indexerNftInfos = new IndexerSeedInfos()
        {
            TotalRecordCount = indexSeedList.Count,
            IndexerSeedInfoList = indexSeedList
        };
        
        mockSeedSymbolSyncedProvider.Setup(calc =>
                calc.GetSeedBriefInfosAsync(It.IsAny<GetCompositeNFTInfosInput>()))
            .ReturnsAsync(
                new Tuple<long, List<SeedSymbolIndex>>(seedsList.Count, seedsList));
         
        mockSeedSymbolSyncedProvider.Setup(calc =>
                calc.GetSeedInfosUserProfileAsync(It.IsAny<GetNFTInfosProfileInput>()))
            .ReturnsAsync(indexerNftInfos);
        return mockSeedSymbolSyncedProvider.Object;
    }

    private static INFTListingProvider MockINFTListingProvider()
    {
        var mockListingProvider = new Mock<INFTListingProvider>();
        mockListingProvider.Setup(calc =>
                calc.GetNFTListingsAsync(It.IsAny<GetNFTListingsDto>()))
            .ReturnsAsync(new PagedResultDto<IndexerNFTListingInfo>());
        return mockListingProvider.Object;
    }
    
    private static IInscriptionProvider MockIInscriptionProvider()
    {
        var mockIInscriptionProvider = new Mock<IInscriptionProvider>();
        mockIInscriptionProvider.Setup(calc =>
                calc.GetIndexerInscriptionInfoAsync(It.IsAny<String>(), It.IsAny<String>()))
            .ReturnsAsync(new InscriptionInfoDto());
        return mockIInscriptionProvider.Object;
    }
}