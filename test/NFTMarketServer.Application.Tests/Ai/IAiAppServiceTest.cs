using System.Collections.Generic;
using AElf.Client.Dto;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NFTMarketServer.Common.AElfSdk;
using NFTMarketServer.Common.Http;
using NFTMarketServer.File;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Redis;
using NFTMarketServer.Users;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace NFTMarketServer.Ai;

public class IAiAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly IAiAppService _aiAppService;
    public IAiAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _aiAppService = GetRequiredService<IAiAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockIOpenAiRedisTokenBucket());
        services.AddSingleton(MockIUserAppService());
        services.AddSingleton(MockIContractProvider());
        services.AddSingleton(MockChainOptionsOptions());
        services.AddSingleton(MockOpenAiOptions());
        services.AddSingleton(MockIHttpService());
        services.AddSingleton(MockISymbolIconAppService());
    }
    
    [Fact(Skip = "This test is skipped")]
    public async void TestCreateAiArtAsync()
    {
        var input = new CreateAiArtInput()
        {
            RawTransaction = "0a220a2048d657339e3046765001266ac666a930af4a9828e3b2b748bd4aa6e51dc5a2fb12220a2088881d4350a8c77c59a42fc86bbcd796b129e086da7e61d24fb86a6cbb6b2f3b18f4f2dc39220476f4de552a124d616e61676572466f727761726443616c6c32760a220a200548ca657f48982cba770e620a3f8932a6bb53c321026876f055b4cca0ff91e012220a20838138b89acb974d85e3acadcf04f056d06d655da910ce6c377232b63cff51ab1a0943726561746541727422210a036361741a0864616c6c2d652d3232073235367832353638024205506978656c82f10441cbdaa3eb587b37cbc641955726f582de5a9306b1f9c96a9ae8a8f84212d2525c7f4bdaf36fa88fa6fa17a4712a99e68d9557ff1aea6b4f23003fd3402cb3251d01",
            ChainId = "tDVW"
        };
        var result = await _aiAppService.CreateAiArtAsync(input);
        result.TotalCount.ShouldBe(1);
    }
    
    private static ISymbolIconAppService MockISymbolIconAppService(){
        var mock = new Mock<ISymbolIconAppService>();
        mock.Setup(cals => cals.UpdateNFTIconWithHashAsync(It.IsAny<byte[]>(),It.IsAny<string>())).ReturnsAsync(new KeyValuePair<string,string>("pairkey","pairValue"));

        return mock.Object;
    }
    
    private static IHttpService MockIHttpService(){
        var mock = new Mock<IHttpService>();

        var result = new OpenAiImageGenerationResponse
        {
            Data = new List<OpenAiImageGeneration>()
            {
                new OpenAiImageGeneration
                {
                    Url = "openAiUrl"
                }
            }
        };
        
        mock.Setup(cals => cals.SendPostRequest(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<Dictionary<string, string>>(),It.IsAny<int>())).ReturnsAsync(JsonConvert.SerializeObject(result));

        mock.Setup(cals => cals.DownloadImageAsUtf8BytesAsync(It.IsAny<string>())).ReturnsAsync(new byte[]{});
        
        return mock.Object;
    }
    
    private static IOptionsMonitor<OpenAiOptions> MockOpenAiOptions(){
        var mock = new Mock<IOptionsMonitor<OpenAiOptions>>();

        var openAiOptions = new OpenAiOptions()
        {
            ImagesUrlV1 = "url",
            ApiKeyList = new List<string>(){"aaa","bbb"},
            DelayMaxTime = 1,
            DelayMillisecond = 1000,
            RepeatRequestIsOn = false
        };
        mock.Setup(x => x.CurrentValue).Returns(openAiOptions); 
        return mock.Object;
    }
    private static IOptionsMonitor<ChainOptions> MockChainOptionsOptions(){
        var mock = new Mock<IOptionsMonitor<ChainOptions>>();

        var chainOptions = new ChainOptions()
        {
            ChainInfos = new Dictionary<string, ChainInfo>()
            {
                {"tDVW",new ChainInfo()
                {
                    ForestContractAddress = "zv7YnQ2dLM45ssfifN1dpwqBwdxH13pqGm9GDH6peRdH8F3hD",
                    CaContractAddress = "238X6iw1j8YKcHvkDYVtYVbuYk2gJnK8UoNpVCtssynSpVC8hb"
                }}
            }
        };
        mock.Setup(x => x.CurrentValue).Returns(chainOptions); 
        return mock.Object;
    }
    private static IOpenAiRedisTokenBucket MockIOpenAiRedisTokenBucket(){
        var mock = new Mock<IOpenAiRedisTokenBucket>();
        mock.Setup(cals => cals.GetNextToken());
        return mock.Object;
    }
    private static IUserAppService MockIUserAppService(){
        var mock = new Mock<IUserAppService>();
        mock.Setup(cals => cals.GetCurrentUserAddressAsync()).ReturnsAsync("2YcGvyn7QPmhvrZ7aaymmb2MDYWhmAks356nV3kUwL8FkGSYeZ");
        return mock.Object;
    }
    
    private static IContractProvider MockIContractProvider(){
        var mock = new Mock<IContractProvider>();
        mock.Setup(cals => cals.SendTransactionAsync(It.IsAny<string>(),It.IsAny<Transaction>())).ReturnsAsync(new SendTransactionOutput()
        {
            TransactionId = "testMockTransactionId"
        });
        mock.Setup(cals => cals.QueryTransactionResult(It.IsAny<string>(),It.IsAny<string>())).ReturnsAsync(new TransactionResultDto()
        {
            TransactionId = "testMockTransactionId",
            Status = TransactionState.Mined
        });
        return mock.Object;
    }
    
}