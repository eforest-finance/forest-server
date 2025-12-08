using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NFTMarketServer.Bid;
using NFTMarketServer.Dealer.ContractInvoker;
using NFTMarketServer.Grains;
using NFTMarketServer.Grains.Grain.Order;
using NFTMarketServer.Helper;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Options;
using NFTMarketServer.Order.Dto;
using NFTMarketServer.Order.Handler;
using NFTMarketServer.Order.Index;
using NFTMarketServer.Users.Dto;
using NFTMarketServer.Users.Provider;
using NSubstitute;
using Orleans;
using Shouldly;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Order;

public class OrderAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly IOrderAppService _orderAppService;
    private readonly MockGraphQLProvider _graphQlProvider;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IPayCallbackAppService _payCallbackAppService;
    private readonly ILocalEventBus _localEventBus;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IContractInvokerFactory _contractInvokerFactory;
    private ICurrentUser _currentUser;
    private PortkeyOption _portkeyOption;
    private readonly INESTRepository<NFTOrderIndex, Guid> _nftOrderRepository;
    private readonly IClusterClient _clusterClient;
    public OrderAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _orderAppService = GetRequiredService<IOrderAppService>();
        _graphQlProvider = GetRequiredService<MockGraphQLProvider>();
        _userInformationProvider = GetRequiredService<IUserInformationProvider>();
        //_localEventBus = GetRequiredService<ILocalEventBus>();
        _payCallbackAppService = GetRequiredService<IPayCallbackAppService>();
        _portkeyOption = GetRequiredService<IOptionsSnapshot<PortkeyOption>>().Value;
        _nftOrderRepository = GetRequiredService<INESTRepository<NFTOrderIndex, Guid>>();
        _distributedEventBus = GetRequiredService<IDistributedEventBus>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(BuildMockIBidAppService());
    }

    [Fact]
    public async Task<CreateOrderResultDto> CreateOrderSuccessTest()
    {
        await _graphQlProvider.GetSeedInfoAsync("LUCK");
        Login();
        var input = new CreateOrderInput()
        {
            Type = "NFTBuy",
            Symbol = "LUCK-0"
        };
        var resultDto = await _orderAppService.CreateOrderAsync(input);
        var orderDto = await _nftOrderRepository.GetAsync(resultDto.OrderId);
        resultDto.OrderId.ShouldBe(GuidHelper.UniqId(orderDto.UserId.ToString(), orderDto.CreateTime.ToString()));
        orderDto.NftSymbol.ShouldBe(input.Symbol);
        orderDto.MerchantName.ShouldBe(_portkeyOption.Name);
        orderDto.UserId.ShouldBe(_currentUser.Id.Value);
        orderDto.OrderStatus.ShouldBe(OrderStatus.UnPay);
        orderDto.PaymentAmount.ShouldBe(100);
        orderDto.PaymentSymbol.ShouldBe("ELF");
        return resultDto;
    }

    [Fact]
    public async Task PayCallBackSuccessTest()
    {
        await _graphQlProvider.GetSeedInfoAsync("LUCK");
        await Login();
        var input = new CreateOrderInput()
        {
            Type = "NFTBuy",
            Symbol = "LUCK-0"
        };
        var createOrderResultDto = await _orderAppService.CreateOrderAsync(input);
        var callbackInput = new PortkeyCallbackInput()
        {
            MerchantOrderId = createOrderResultDto.OrderId,
            OrderId = createOrderResultDto.ThirdPartOrderId,
            Status = nameof(PortkeyOrderStatus.Pending)
        };
        callbackInput.Signature = SignatureHelper.GetSignature(_portkeyOption.PrivateKey, callbackInput);
        await _payCallbackAppService.PortkeyOrderCallbackAsync(callbackInput);
        var orderIndex = await _nftOrderRepository.GetAsync(createOrderResultDto.OrderId);
        orderIndex.OrderStatus.ShouldBe(OrderStatus.Notifying);
        await _distributedEventBus.PublishAsync(new CreateSeedResultEvent()
        {
            TransactionId = "sssssss",
            Id = createOrderResultDto.OrderId,
            Success = true
        });
        orderIndex = await _nftOrderRepository.GetAsync(createOrderResultDto.OrderId);
        orderIndex.OrderStatus.ShouldBe(OrderStatus.Finished);
        
        var grain = _clusterClient.GetGrain<INFTOrderGrain>(GrainIdHelper.GenerateGrainId(orderIndex.ChainId, orderIndex.Id));
        var orderStatusMap = await grain.GetOrderStatusInfoMapAsync();
        orderStatusMap.Success.ShouldBeTrue();
        orderStatusMap.Data.ShouldContainKey(OrderStatus.Init);
        orderStatusMap.Data.ShouldContainKey(OrderStatus.UnPay);
        orderStatusMap.Data.ShouldContainKey(OrderStatus.Payed);
        orderStatusMap.Data.ShouldContainKey(OrderStatus.Notifying);
        orderStatusMap.Data.ShouldContainKey(OrderStatus.NotifySuccess);
        orderStatusMap.Data.ShouldContainKey(OrderStatus.Finished);
    }

    private async Task Login()
    {
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
        UserSourceInput userSourceInput = new UserSourceInput
        {
            UserId = userId,
            AelfAddress = "24HEphRKXGVt6pPqiETgBbTYp4ksJGJgfrvT5U4z27493imoHU"
        };
        AsyncHelper.RunSync(async () =>
        {
            await _userInformationProvider.SaveUserSourceAsync(userSourceInput);
        });
    }
    
    [Fact]
    public async Task SearchOrderTest()
    {
        Login();
        var input = new CreateOrderInput()
        {
            Type = "NFTBuy",
            Symbol = "LUCK-0"
        };
        var createOrderResultDto = await _orderAppService.CreateOrderAsync(input);
        var orderDto = await _orderAppService.SearchOrderAsync(new SearchOrderInput()
        {
            OrderId = createOrderResultDto.OrderId
        });
        orderDto.NftSymbol.ShouldBe("LUCK-0");
        orderDto.OrderId.ShouldBe(createOrderResultDto.OrderId);
        orderDto.MerchantName.ShouldBe(_portkeyOption.Name);
        orderDto.MerchantOrderId.ShouldBe(createOrderResultDto.ThirdPartOrderId);
        orderDto.OrderStatus.ShouldBe(OrderStatus.UnPay);
    }

    private static IBidAppService BuildMockIBidAppService()
    {
        var mockIBidAppService =
            new Mock<IBidAppService>();
        return mockIBidAppService.Object;
    }
}