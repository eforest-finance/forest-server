using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NFTMarketServer.Synchronize.Dto;
using NFTMarketServer.Synchronize.Provider;
using NFTMarketServer.Tokens;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Synchronize;

public class SynchronizeAppServiceTests : NFTMarketServerApplicationTestBase
{
    private readonly ISynchronizeAppService _synchronizeAppService;
    private const string defaultTxHash = "054d1ff25eef3f31895fe15655711f90f44a849885723c9a57c769d4f260f180";

    public SynchronizeAppServiceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _synchronizeAppService = GetRequiredService<ISynchronizeAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockISynchronizeTransactionProvider());
        services.AddSingleton(BuildMockIBus());
        services.AddSingleton(BuildMockITokenMarketDataProvider());
    }


    [Fact]
    public async Task GetSyncResultByTxHashAsyncTest()
    {
        var input = new GetSyncResultByTxHashDto()
        {
            TxHash = defaultTxHash,
        };

        var result = await _synchronizeAppService.GetSyncResultByTxHashAsync(input);
        result.ShouldNotBeNull();
        result.TxHash.Equals(defaultTxHash);
        result.Status.ShouldBe("Failed");
        
        var result2 = await _synchronizeAppService.GetSyncResultForAuctionSeedByTxHashAsync(input);
        result2.ShouldNotBeNull();
    }
    
    private ISynchronizeTransactionProvider MockISynchronizeTransactionProvider()
    {
        var mock = new Mock<ISynchronizeTransactionProvider>();

        var result1Str =
            "{\n  \"FromChainId\": \"AELF\",\n  \"ToChainId\": \"tDVW\",\n  \"Symbol\": \"TANGTANGCHENN-0\",\n  \"TxHash\": \"054d1ff25eef3f31895fe15655711f90f44a849885723c9a57c769d4f260f180\",\n  \"ValidateTokenHeight\": 0,\n  \"Message\": \"Transaction failed, status: ProxyAccountsAndTokenCreating. error: \",\n  \"Status\": \"Failed\"\n}";
        mock.Setup(calc =>
                calc.GetSynchronizeJobByTxHashAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(
                JsonConvert.DeserializeObject<SynchronizeTransactionDto>(result1Str));

        var result2Str =
            "[\n  {\n    \"FromChainId\": \"AELF\",\n    \"ToChainId\": \"tDVW\",\n    \"Symbol\": \"OVEJKQMDEM-0\",\n    \"TxHash\": \"e414b8aebc4141e8014a66caceceb7be7d2189d18ba0234721fd72428f91efa0\",\n    \"ValidateTokenTxId\": \"f7b181692029e9b0394823669c4047135c199829e05a162e83f8a723e7e7bdb0\",\n    \"ValidateTokenTx\": \"0a220a20777aec9e2a5204f80a582c217cac2cfbc70ab3c5b6ea7f5d7d6786405d718b1712220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc18a3a49c0a2204134a39d42a1756616c6964617465546f6b656e496e666f45786973747332ef020a0c4f56454a4b514d44454d2d301202574818012a220a2086ec73e4d99927f85177d0cfe5c8809218a13bb6ee0ee6e801c6f000f300e89a30013898f57542350a0f5f5f6e66745f66696c655f6861736812222235373132386133616162636231313435303661303265313631333631303230662242670a0e5f5f6e66745f66696c655f75726c125568747470733a2f2f666f726573742d6465762e73332e61702d6e6f727468656173742d312e616d617a6f6e6177732e636f6d2f313639313034313637313834312d6f7574707574253230253238342532392e706e6742380a125f5f6e66745f666561747572655f68617368122222336233616365353266303036323164613639616433636634373933376430386122421b0a145f5f6e66745f7061796d656e745f746f6b656e731203454c4642140a0e5f5f6e66745f6d6574616461746112025b5d4a220a2006837fd536d9e9d361bcbf85bf6c94ac6badacea9ae75d1b48c95a6c6e2cf15782f1044146c813528d418385feb7ff897f46c37bf4e6f9a3e1df4899051403692c9634731aefcc61088e911ed52aaae74878809ef785013b30c167c7abf51045f999503b00\",\n    \"ValidateTokenHeight\": 21434917,\n    \"CrossChainCreateTokenTxId\": \"0fd0755edb2d69446c67607facfad96193e1909909d99a91ff379771396e8f3f\",\n    \"Status\": \"CrossChainTokenCreated\"\n  },\n  {\n    \"FromChainId\": \"string\",\n    \"ToChainId\": \"string\",\n    \"Symbol\": \"string\",\n    \"TxHash\": \"string\",\n    \"ValidateTokenHeight\": 0,\n    \"Status\": \"Failed\"\n  }\n]";
        return mock.Object;
    }
    
    
    private static IBus BuildMockIBus()
    {
        var mockIBus =
            new Mock<IBus>();
        return mockIBus.Object;
    } 
    private static ITokenMarketDataProvider BuildMockITokenMarketDataProvider()
    {
        var mockITokenMarketDataProvider =
            new Mock<ITokenMarketDataProvider>();
        return mockITokenMarketDataProvider.Object;
    }

}