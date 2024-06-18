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
using NFTMarketServer.Message;
using NFTMarketServer.Redis;
using NFTMarketServer.Users;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace NFTMarketServer.Ai;

public class MessageServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly IMessageService _messageService;
    public MessageServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _messageService = GetRequiredService<IMessageService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);

    }
    
    [Fact(Skip = "This test is skipped")]
    public async void TestGetMessageListAsync()
    {
       
        var result = await _messageService.GetMessageListAsync(new QueryMessageListInput());
        result.TotalCount.ShouldBe(1);
    }
    
   
}