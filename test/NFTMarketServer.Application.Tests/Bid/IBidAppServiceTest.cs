using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.BId.Index;
using NFTMarketServer.Common;
using NFTMarketServer.Tokens;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Bid;

public class IBidAppServiceTest: NFTMarketServerApplicationTestBase
{
    private readonly BidAppService _bidAppService;
    private readonly INESTRepository<SymbolBidInfoIndex, string> _symbolBidInfoIndexRepository;
    private readonly INESTRepository<SymbolAuctionInfoIndex, string> _symbolAuctionInfoIndexRepository;

    public IBidAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _symbolBidInfoIndexRepository = GetRequiredService<INESTRepository<SymbolBidInfoIndex, string>>();
        _symbolAuctionInfoIndexRepository = GetRequiredService<INESTRepository<SymbolAuctionInfoIndex, string>>();
        _bidAppService = GetRequiredService<BidAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockIGraphQLHelper());
        services.AddSingleton(MockITokenAppService());
    }
    
    private async Task Init()
    {
        var symbolBidInfoIndex = new SymbolBidInfoIndex()
        {
            Id = "1",
            AuctionId = "1",
            Bidder = "LQWEmSYZo75oALWb3xYZbrvyXaxTSbfqDtUK2YozW1YorQAMk",
            BidTime = new DateTime().Millisecond,
            BlockHeight = 1,
            PriceSymbol = "ELF",
            PriceAmount = 1,
            Symbol = "AAA",
            TransactionHash = "hash"
        };
        await _symbolBidInfoIndexRepository.AddOrUpdateAsync(symbolBidInfoIndex);

        var symbolAuctionInfoIndex = new SymbolAuctionInfoIndex()
        {
            Id = "1",
            Symbol = "AAA",
            CollectionSymbol = "SEED=0",
            FinishBidder = "LQWEmSYZo75oALWb3xYZbrvyXaxTSbfqDtUK2YozW1YorQAMk",
            StartTime = DateTime.Now.Millisecond,
            EndTime =  DateTime.Now.Millisecond,
            TransactionHash = "hash",
            FinishIdentifier =0,
            
        };
        await _symbolAuctionInfoIndexRepository.AddOrUpdateAsync(symbolAuctionInfoIndex);
    }

    [Fact]
    public async void TestGetSymbolBidInfoListAsync()
    {
        await Init();
        var input = new GetSymbolBidInfoListRequestDto()
        {
            SeedSymbol = "AAA",
            SkipCount = 0,
            MaxResultCount = 1
        };
        var result = await _bidAppService.GetSymbolBidInfoListAsync(input);
        result.ShouldNotBe(null);
        result.TotalCount.ShouldBe(1);
        result.Items[0].SeedSymbol.ShouldBe("AAA");
    }
    [Fact]
    public async void TestGetSymbolBidInfoAsync()
    {
        await Init();
        var symbol = "AAA";
        var transactionHash = "hash";
        var result = await _bidAppService.GetSymbolBidInfoAsync(symbol, transactionHash);
        result.ShouldNotBe(null);
        result.Id.ShouldBe("1");
        
    }
    [Fact]
    public async void TestGetSymbolAuctionInfoListAsync()
    {
        await Init();
        var seedSymbol = "AAA";
        var result = await _bidAppService.GetSymbolAuctionInfoListAsync(seedSymbol);
        result.ShouldNotBe(null);
        result.Count.ShouldBe(1);
        result[0].SeedSymbol.ShouldBe("AAA");
    }
    [Fact]
    public async void TestGetSymbolAuctionInfoAsync()
    {
        await Init();
        var seedSymbol = "AAA";
        var result = await _bidAppService.GetSymbolAuctionInfoAsync(seedSymbol);
        result.ShouldNotBe(null);
        result.Id.ShouldBe("1");
        result.SeedSymbol.ShouldBe("AAA");
    }
    [Fact]
    public async void TestGetUnFinishedSymbolAuctionInfoListAsync()
    {
        await Init();
        var input = new GetAuctionInfoRequestDto()
        {
            SkipCount = 0,
            MaxResultCount = 1
        };
        var result = await _bidAppService.GetUnFinishedSymbolAuctionInfoListAsync(input);
        result.ShouldNotBe(null);
        result.Count.ShouldBe(1);
        result[0].SeedSymbol.ShouldBe("AAA");
    }
    [Fact]
    public async void TestGetSymbolAuctionInfoByIdAsync()
    {
        await Init();
        var auctionId = "1";
        var result = await _bidAppService.GetSymbolAuctionInfoByIdAsync(auctionId);
        result.ShouldNotBe(null);
        result.Id.ShouldBe("1");
        result.SeedSymbol.ShouldBe("AAA");
    }
    [Fact]
    public async void TestGetSymbolAuctionInfoByIdAndTransactionHashAsync()
    {
        await Init();
        var auctionId = "1";
        var transactionHash = "hash";
        var result = await _bidAppService.GetSymbolAuctionInfoByIdAndTransactionHashAsync(auctionId, transactionHash);
        result.ShouldNotBe(null);
        result.Id.ShouldBe("1");
        result.SeedSymbol.ShouldBe("AAA");
    }
    [Fact]
    public async void TestGetSeedAuctionInfoAsync()
    {
        await Init();
        var symbol = "AAA";
        var result = await _bidAppService.GetSeedAuctionInfoAsync(symbol);
        result.ShouldNotBe(null);
        result.BidderList.Count.ShouldBe(1);
        result.BidderList[0].ShouldBe("LQWEmSYZo75oALWb3xYZbrvyXaxTSbfqDtUK2YozW1YorQAMk");
        result.TopBidPrice.ShouldNotBe(null);
        result.TopBidPrice.Amount.ShouldBe(1);
    }
    
    private static IGraphQLHelper MockIGraphQLHelper()
    {
        var result =
            "";
        var mockIGraphQLHelper = new Mock<IGraphQLHelper>();
            
        mockIGraphQLHelper.Setup(cals => cals.QueryAsync<Object>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(JsonConvert.DeserializeObject<Object>(result));
        return mockIGraphQLHelper.Object;
    }

    private static ITokenAppService MockITokenAppService()
    {
        var mockITokenAppService = new Mock<ITokenAppService>();
            
        mockITokenAppService.Setup(cals => cals.GetCurrentDollarPriceAsync(It.IsAny<string>(),It.IsAny<decimal>()))
            .ReturnsAsync(new decimal(1));
        return mockITokenAppService.Object;
    }
    
}