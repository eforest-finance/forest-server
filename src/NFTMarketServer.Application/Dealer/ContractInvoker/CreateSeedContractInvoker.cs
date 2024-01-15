using System;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Types;
using Forest.SymbolRegistrar;
using Google.Protobuf;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Provider;
using NFTMarketServer.Order.Handler;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Dealer.ContractInvoker;

public class CreateSeedContractInvoker : AbstractContractInvoker
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;

    public CreateSeedContractInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, IObjectMapper objectMapper) : base(distributedEventBus,
        contractInvokeProvider, objectMapper)
    {
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
    }

    public override string BizType()
    {
        return Dtos.BizType.CreateSeed.ToString();
    }

    public override async Task<ContractParamDto> AdaptToContractParamAsync<TCreateSeedBizDto>(TCreateSeedBizDto invokeBizDto)
    {
        AssertHelper.NotNull(invokeBizDto, "CreateSeedBizDto empty");
        AssertHelper.NotNull(invokeBizDto is CreateSeedBizDto, "Invalid CreateSeedBizDto type");
        
        var createSeedDto = invokeBizDto as CreateSeedBizDto;
        AssertHelper.NotNull(createSeedDto, "createSeedBizDto empty");

        var contractParamDto = new ContractParamDto
        {
            BizId = createSeedDto?.OrderId,
            BizType = BizType(),
            ChainId = DealerContractType.MainChainId,
            ContractName = DealerContractType.RegistrarContractName,
            ContractMethod = DealerContractType.RegistrarCreateSeedMethod,
            Sender = DealerContractType.RegistrarCreateSeedAccount,
            BizData = new CreateSeedInput
            {
                Symbol = createSeedDto?.Symbol,
                To = Address.FromBase58(createSeedDto?.Address)
            }.ToByteString().ToBase64()
        };
        return contractParamDto;
    }

    public override async Task ResultCallbackAsync(string bizId, bool invokeSuccess, TransactionResultDto result,
        string rawTransaction)
    {
        await _distributedEventBus.PublishAsync(new CreateSeedResultEvent()
        {
            Id = Guid.Parse(bizId),
            TransactionId = result.TransactionId,
            Success = invokeSuccess
        });
    }
}