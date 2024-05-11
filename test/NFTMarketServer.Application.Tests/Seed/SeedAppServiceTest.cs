using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.Seed.Provider;
using NFTMarketServer.Tokens;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Seed;

public class SeedAppServiceTest: NFTMarketServerApplicationTestBase
{
    private readonly ISeedAppService _seedAppService;
    private readonly IBidAppService _bidAppService;
    
    public SeedAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _seedAppService = GetRequiredService<ISeedAppService>();
        _bidAppService = GetRequiredService<IBidAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockISeedProvider());
        services.AddSingleton(MockITokenMarketDataProvider());
        services.AddSingleton(MockTsmSeedProvider());
    }
    
    [Fact]
    public async Task GetSpecialSymbolListAsync_Test()
    {
        var result = await
            _seedAppService.GetSpecialSymbolListAsync(new QuerySpecialListInput
            {
                SkipCount = 0,
                MaxResultCount = 10
            });
        
        TestOutputHelper.WriteLine(JsonConvert.SerializeObject(result));
        result.Items.Count.ShouldBe(0);
    }

    [Fact]
    public async Task AddOrUpdateTsmSeedInfoAsync_Test()
    {
        var seedInfo = new SeedDto()
        {
            Id = "tDVV-SYB",
            Symbol = "SYB",
            ChainId = "tDVV",
            SeedSymbol = "SEED-007",
            SeedName = "SEED-SYB",
            Status = SeedStatus.AVALIABLE,
            TokenType = TokenType.FT.ToString(),
            SeedType = SeedType.UNIQUE,
            AuctionType = AuctionType.Dutch,
            TokenPrice = new TokenPriceDto()
            {
                Symbol = "ELF",
                Amount = 10000000000
            },
            BlockHeight = 11111,
            IsBurned = false,
            AuctionStatus = 0
        };
        await _seedAppService.AddOrUpdateTsmSeedInfoAsync(seedInfo);
        var result = await _seedAppService.GetSpecialSymbolListAsync(new QuerySpecialListInput()
        {
            TokenTypes = new List<TokenType>()
            {
                TokenType.FT
            },
            SeedTypes = new List<SeedType>()
            {
                SeedType.UNIQUE
            }
        });
        result.Items.Count.ShouldBe(1);
        result.Items[0].Symbol.ShouldBe("SYB");
    }

    [Fact]
    public async Task UpdateSeedRankingWeightAsync_Test()
    { 
        await AddOrUpdateTsmSeedInfoAsync_Test();
        var rankingWeightInfos = await _seedAppService.GetSeedRankingWeightInfosAsync();
        rankingWeightInfos.Items.Count.ShouldBe(0);

        var input = new List<SeedRankingWeightDto>()
        {
            new SeedRankingWeightDto()
            {
                Symbol = "SYB",
                RankingWeight = 899
            }
        };
        await _seedAppService.UpdateSeedRankingWeightAsync(input);
        var result = await _seedAppService.GetSeedRankingWeightInfosAsync();
        result.Items.Count.ShouldBe(1);
        result.Items[0].Symbol.ShouldBe("SYB");
        result.Items[0].RankingWeight.ShouldBe(899);
    }
    
    
    [Fact]
    public async Task GetBiddingSeedsAsync_ReturnsSortedResults()
    {
        // Arrange
        var input = new GetBiddingSeedsInput();
          

       // Act
       var result = await _seedAppService.GetBiddingSeedsAsync(input);

       // Assert
       Assert.NotNull(result);
       Assert.Equal(5, result.TotalCount);
       Assert.All(result.Items, item => Assert.IsType<BiddingSeedDto>(item));
    }
    

    [Fact]
    public async Task GetSymbolBidPriceAsync_Test()
    {
        //Add symbol auction info
        var auctionInfoDtoAdd = new AuctionInfoDto()
        {
            Id = 12306.ToString(),
            SeedSymbol = "SEED-40",
            EndTime = DateTimeOffset.Now.AddDays(1).ToUnixTimeMilliseconds(),
            StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            MinMarkup = 20,
        };
        await _bidAppService.AddSymbolAuctionInfoListAsync(auctionInfoDtoAdd);
        
        //Add symbol bid info
        var bidInfoDtoAdd = new BidInfoDto()
        {
            Id = Guid.NewGuid().ToString(),
            SeedSymbol = "SEED-40",
            BlockHeight = 123060000L,
            TransactionHash = "1",
            PriceSymbol = "ELF",
            PriceAmount = 660
        };
        await _bidAppService.AddBidInfoListAsync(bidInfoDtoAdd);
        
        var result = await
            _seedAppService.GetSymbolBidPriceAsync(new QueryBidPricePayInfoInput
            {
                Symbol = "SEED-40"
            });
        result.ShouldNotBeNull();
        result.ElfBidPrice.ShouldBe(0);
        result.MinElfPriceMarkup.ShouldBe(0);
        result.MinMarkup.Equals(0.002);
        
    }
    
    private static ISeedProvider MockISeedProvider()
    {
        var result =
            "{\"TotalRecordCount\":3,\"IndexerSpecialSeedList\":[" +
            "{\"Symbol\":\"NZC-0\",\"SeedName\":\"SEED-NZC-0\",\"BidderList\":[],\"AuctionType\":1,\"TokenPrice\":{\"Symbol\":\"ELF\",\"Amount\":200},\"AuctionEndTime\":1792145642,\"Status\":0,\"SeedType\":2}," +
            "{\"Symbol\":\"WEDSJPNEWFM-0\",\"SeedName\":\"SEED-WEDSJPNEWFM-0\",\"BidderList\":[\"2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk\"],\"AuctionType\":0,\"TokenPrice\":{\"Symbol\":\"ELF\",\"Amount\":500},\"AuctionEndTime\":1692145642,\"Status\":0,\"SeedType\":3}," +
            "{\"Symbol\":\"XYZ-0\",\"SeedName\":\"SEED-XYZ-0\",\"BidderList\":[\"2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk\"],\"AuctionType\":2,\"TokenPrice\":{\"Symbol\":\"ELF\",\"Amount\":800},\"AuctionEndTime\":1691145642,\"Status\":0,\"SeedType\":2}],\"Data\":null}";

        // var test = JsonConvert.DeserializeObject<IndexerSpecialSeeds>(result);
        // Console.WriteLine(JsonConvert.SerializeObject(test));
        // var mockISeedProvider =
        //     new Mock<ISeedProvider>();
        // mockISeedProvider.Setup(calc =>
        //     calc.GetSpecialSeedsAsync(new QuerySpecialListInput()
        //     {
        //         SkipCount = 0,
        //         MaxResultCount = 10,
        //     })).ReturnsAsync(
        //     JsonConvert
        //         .DeserializeObject<
        //             IndexerSpecialSeeds>(
        //             result));
        var mockISeedProvider =
            new Mock<ISeedProvider>();
        mockISeedProvider.Setup(calc =>
            calc.GetSpecialSeedsAsync(new QuerySpecialListInput()
            {
                SkipCount = 0,
                MaxResultCount = 10,
            })).ReturnsAsync(new IndexerSpecialSeeds()
        {
            TotalRecordCount = 3,
            IndexerSpecialSeedList = new List<SpecialSeedItem>()
            {
                new SpecialSeedItem(){Symbol="NZC-0"},
                new SpecialSeedItem(){Symbol = "WEDSJPNEWFM-0"},
                new SpecialSeedItem(){Symbol = "XYZ-0"}
            }
        });
        return mockISeedProvider.Object;
    }
    
    public ITokenMarketDataProvider MockITokenMarketDataProvider()
    {
        var mockService = new Mock<ITokenMarketDataProvider>();
        mockService.Setup(calc =>
                calc.GetPriceAsync(
                    It.IsAny<string>()))
            .ReturnsAsync(new Decimal(1));
        mockService.Setup(calc =>
                calc.GetHistoryPriceAsync(
                    It.IsAny<string>(),It.IsAny<DateTime>()))
            .ReturnsAsync(new Decimal(1));
        return mockService.Object;

    }
    
    private ITsmSeedProvider MockTsmSeedProvider()
    {
        var mockTsmSeedProvider = new Mock<ITsmSeedProvider>();
        var input = new GetBiddingSeedsInput();
        var seeds = new List<TsmSeedSymbolIndex>
        {
            // add test data
            new () { Id = "1", TopBidPrice = new TokenPriceDto() { Amount = 16 }, BiddersCount  = 9, AuctionEndTime = 11},
            new () { Id = "2", TopBidPrice = new TokenPriceDto() { Amount = 13 }, BiddersCount  = 9, AuctionEndTime = 11},
            new () { Id = "3", TopBidPrice = new TokenPriceDto() { Amount = 13 }, BiddersCount  = 8, AuctionEndTime = 11},
            new () { Id = "4", TopBidPrice = new TokenPriceDto() { Amount = 17 }, BiddersCount  = 8, AuctionEndTime = 6},
            new () { Id = "5", TopBidPrice = new TokenPriceDto() { Amount = 17 }, BiddersCount  = 8, AuctionEndTime = 5}
        };
        mockTsmSeedProvider.Setup(service => service.GetBiddingSeedsAsync(
                It.IsAny<GetBiddingSeedsInput>(), 
                It.IsAny<Expression<Func<TsmSeedSymbolIndex, object>>>(), 
                It.IsAny<SortOrder>()))
            .ReturnsAsync(Tuple.Create<long, List<TsmSeedSymbolIndex>>(seeds.Count, seeds));

        return mockTsmSeedProvider.Object;
    }
}