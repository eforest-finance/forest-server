using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NFTMarketServer.Message;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Ai;

public class MessageServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly IMessageService _messageService;
    private static INESTRepository<MessageInfoIndex, string> _messageInfoIndexRepository;

    public MessageServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _messageService = GetRequiredService<IMessageService>();
        _messageInfoIndexRepository = GetRequiredService<INESTRepository<MessageInfoIndex, string>>();

    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);

    }
    
    [Fact(Skip = "This test is skipped")]
    public async void TestGetMessageListAsync()
    { 
        await MockMessageInfoAsync();
        var result = await _messageService.GetMessageListAsync(new QueryMessageListInput());
        result.TotalCount.ShouldBe(2);
        var resultJson = JsonConvert.SerializeObject(result);
        result.Items[0].Address.ShouldBe("T7ApxUrF6vYfBizHBLSrfiEgEEZH2yURp3stye5AJLyc2F96z");
        result.Items[0].Title.ShouldBe("SGR-1");
        result.Items[1].Title.ShouldBe("SGR-2");

        
    }

    private  async Task  MockMessageInfoAsync()
    {
        var message1 = new MessageInfoIndex()
        {
            Id = "123",
            Address = "T7ApxUrF6vYfBizHBLSrfiEgEEZH2yURp3stye5AJLyc2F96z",
            Title = "SGR-1",
            Body = "SEED-0",
            Utime = new DateTime(),
            Ctime = new DateTime(),
            Status = 0,
            SinglePrice = "1",
            PriceType = "ELF",
            Amount = "3",
            TotalPrice = "3",
            Image = "",
            BusinessId = "SGR-1",
            BusinessType = 0,
            SecondLevelType =0,
            Decimal = 0,
            
        };
       
        await _messageInfoIndexRepository.AddAsync(message1);
        Thread.Sleep(1000);
        var message2 = new MessageInfoIndex()
        {
            Id = "456",
            Address = "T7ApxUrF6vYfBizHBLSrfiEgEEZH2yURp3stye5AJLyc2F96z",
            Title = "SGR-2",
            Body = "SEED-0",
            Utime = new DateTime(),
            Ctime = new DateTime(),
            Status = 0,
            SinglePrice = "1",
            PriceType = "ELF",
            Amount = "3",
            TotalPrice = "3",
            Image = "",
            BusinessId = "SGR-2",
            BusinessType = 0,
            SecondLevelType =0,
            Decimal = 0,
            
        };
        await _messageInfoIndexRepository.AddAsync(message2);
        Thread.Sleep(3000);

    }


}