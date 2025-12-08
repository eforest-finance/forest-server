using System.Net.Http;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Dealer.ContractInvoker;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Provider;
using NFTMarketServer.Dealer.Worker;
using NFTMarketServer.Tokens;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Dealer;

public partial class ContractInvokerTest : NFTMarketServerApplicationTestBase
{
    private readonly IContractInvokerFactory _contractInvokerFactory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ContractInvokeProvider _contractInvokeProvider;
    private IContractInvokerWorker _contractInvokerWorker;

    public ContractInvokerTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _contractInvokerFactory = ServiceProvider.GetRequiredService<IContractInvokerFactory>();
        _contractInvokeProvider = ServiceProvider.GetRequiredService<ContractInvokeProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockChainOptions());
        services.AddSingleton(MockHttpFactory(_testOutputHelper,
            PathMatcher(HttpMethod.Post, "api/blockChain/sendTransaction", new SendTransactionOutput())
        ));
        services.AddSingleton(MockSendTransaction(TransactionResultStatus.PENDING.ToString()));
        services.AddSingleton(BuildMockIBus());
        services.AddSingleton(BuildMockITokenMarketDataProvider());
    }


    [Fact]
    public async Task CreateContractInvoke()
    {       
        await _contractInvokerFactory
            .Invoker(BizType.AuctionClaim.ToString())
            .InvokeAsync(new AuctionInfoDto
            {
                Id = HashHelper.ComputeFrom("LUCK").ToHex()
            });

        var grain = await _contractInvokeProvider.GetByIdAsync(BizType.AuctionClaim.ToString(),
            HashHelper.ComputeFrom("LUCK").ToHex());
        grain.ShouldNotBeNull();
        grain.Status.ShouldBe(ContractInvokeSendStatus.Sent.ToString());
        grain.TransactionStatus.ShouldBe(TransactionResultStatus.PENDING.ToString());

        // update to MINED transaction, let mock client returns MINED result
        grain.TransactionId = HashHelper.ComputeFrom(TransactionResultStatus.MINED.ToString()).ToHex();
        await _contractInvokeProvider.AddUpdateAsync(grain);
        
        _contractInvokerWorker = ServiceProvider.GetRequiredService<IContractInvokerWorker>();
        await _contractInvokerWorker.Invoke();
        

        grain = await _contractInvokeProvider.GetByIdAsync(BizType.AuctionClaim.ToString(),
            HashHelper.ComputeFrom("LUCK").ToHex());
        grain.ShouldNotBeNull();
        grain.Status.ShouldBe(ContractInvokeSendStatus.Success.ToString());
        grain.TransactionStatus.ShouldBe(TransactionResultStatus.MINED.ToString());

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