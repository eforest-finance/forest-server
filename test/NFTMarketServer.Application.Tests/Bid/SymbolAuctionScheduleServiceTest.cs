using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Chain;
using NFTMarketServer.Provider;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Worker;

public class SymbolAuctionScheduleServiceTest : NFTMarketServerApplicationTestBase
{
    private const string CHAIN_ID = "AELF";

    private readonly ITestOutputHelper _testOutputHelper;

    public SymbolAuctionScheduleServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }


    // [Fact]
    // public async Task SyncSymbolAuction_LastEndHeight_Greater_Or_Equal_To_NewIndexHeight()
    // {
    //     var _bidAppService = Substitute.For<IBidAppService>();
    //     var _graphQlProvider = Substitute.For<IGraphQLProvider>();
    //     _graphQlProvider.GetLastEndHeightAsync(CHAIN_ID, BusinessQueryChainType.SymbolAuction)
    //         .Returns(Task.FromResult(101L));
    //     _graphQlProvider.GetIndexBlockHeightAsync(CHAIN_ID).Returns(Task.FromResult(100L));
    //
    //
    //     var _symbolAuctionScheduleService = new SymbolAuctionScheduleService(
    //         ServiceProvider.GetRequiredService<ILogger<SymbolAuctionScheduleService>>(),
    //         _graphQlProvider, _bidAppService);
    //
    //     //Act
    //     await _symbolAuctionScheduleService.SyncSymbolAuctionRecordsAsync(CHAIN_ID);
    //     
    //     //Assert
    //     await _graphQlProvider.Received(0).GetSyncSymbolAuctionRecordsAsync(CHAIN_ID, 101L, 0);
    // }
    //
    // [Fact]
    // public async Task SyncSymbolAuction_LastEndHeight_Less_Than_NewIndexHeight_And_AuctionRecords_Not_Empty()
    // {
    //     var _bidAppService = Substitute.For<IBidAppService>();
    //     var _graphQlProvider = Substitute.For<IGraphQLProvider>();
    //
    //     var _blockHeight = 1000L;
    //     var _id = "101";
    //     _graphQlProvider.GetLastEndHeightAsync(CHAIN_ID, BusinessQueryChainType.SymbolAuction)
    //         .Returns(Task.FromResult(100L));
    //     _graphQlProvider.GetIndexBlockHeightAsync(CHAIN_ID).Returns(Task.FromResult(101L));
    //     var _auctionInfoDtoAdd = new AuctionInfoDto
    //     {
    //         Id = "102",
    //         EndTime = 1694487788000
    //     };
    //     _graphQlProvider.GetSyncSymbolAuctionRecordsAsync(CHAIN_ID, 100L, 0)
    //         .Returns(Task.FromResult(new List<AuctionInfoDto>
    //         {
    //             new AuctionInfoDto
    //             {
    //                 Id = _id,
    //                 BlockHeight = _blockHeight
    //             },
    //             _auctionInfoDtoAdd 
    //         }));
    //     var _auctionInfoDto = new AuctionInfoDto
    //     {
    //         EndTime = 1694487788000
    //     };
    //     _bidAppService.GetSymbolAuctionInfoByIdAsync(_id).Returns(Task.FromResult(_auctionInfoDto));
    //
    //     var _symbolAuctionScheduleService = new SymbolAuctionScheduleService(
    //         ServiceProvider.GetRequiredService<ILogger<SymbolAuctionScheduleService>>(),
    //         _graphQlProvider, _bidAppService);
    //     //Act
    //     await _symbolAuctionScheduleService.SyncSymbolAuctionRecordsAsync(CHAIN_ID);
    //     
    //     //Assert
    //     //check modify
    //     await _bidAppService.Received(1).UpdateSymbolAuctionInfoAsync(_auctionInfoDto);
    //     //check add 
    //     await _bidAppService.Received(1).AddSymbolAuctionInfoListAsync(_auctionInfoDtoAdd);
    //     await _graphQlProvider.Received(1).SetLastEndHeightAsync(CHAIN_ID, BusinessQueryChainType.SymbolAuction, _blockHeight);
    //
    // }
    //
    //
    // [Fact]
    // public async Task SyncSymbolBidRecordsAsync()
    // {
    //     var _bidAppService = Substitute.For<IBidAppService>();
    //     var _graphQlProvider = Substitute.For<IGraphQLProvider>();
    //
    //     var _blockHeight = 1000L;
    //     var _lastEndHeight = 100L;
    //     var _id = "101";
    //     var _symbol = "ELF";
    //     _graphQlProvider.GetLastEndHeightAsync(CHAIN_ID, BusinessQueryChainType.SymbolBid)
    //         .Returns(Task.FromResult(_lastEndHeight));
    //     _graphQlProvider.GetIndexBlockHeightAsync(CHAIN_ID).Returns(Task.FromResult(101L));
    //
    //     var _bidInfoDto = new BidInfoDto
    //     {
    //         Id = _id,
    //         Symbol = _symbol,
    //         BlockHeight = _blockHeight,
    //         TransactionHash = "1"
    //     };
    //     _graphQlProvider.GetSyncSymbolBidRecordsAsync(CHAIN_ID, 100L, 0)
    //         .Returns(Task.FromResult(new List<BidInfoDto>
    //         {
    //             _bidInfoDto
    //         }));
    //     
    //     _bidAppService.GetSymbolBidInfoAsync(_symbol, "2");
    //     
    //     var _symbolAuctionScheduleService = new SymbolAuctionScheduleService(
    //         ServiceProvider.GetRequiredService<ILogger<SymbolAuctionScheduleService>>(),
    //         _graphQlProvider, _bidAppService);
    //
    //     //Act
    //     await _symbolAuctionScheduleService.SyncSymbolBidRecordsAsync(CHAIN_ID);
    //     
    //     //check add
    //     await _bidAppService.Received(1).AddBidInfoListAsync(_bidInfoDto);
    //     
    //     await _graphQlProvider.Received(1).SetLastEndHeightAsync(CHAIN_ID, BusinessQueryChainType.SymbolBid, _blockHeight);
    //
    // }
}