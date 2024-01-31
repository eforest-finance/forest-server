using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Types;
using Forest.Contracts.Drop;
using Google.Protobuf;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Provider;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace NFTMarketServer.Dealer.ContractInvoker;

public class NFTDropFinishInvoker : AbstractContractInvoker
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<NFTDropFinishInvoker> _logger;

    public NFTDropFinishInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, 
        IObjectMapper objectMapper,
        ILogger<NFTDropFinishInvoker> logger) : base(distributedEventBus,
        contractInvokeProvider, objectMapper)
    {
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _logger = logger;
    }

    public override string BizType()
    {
        return Dtos.BizType.NFTDropFinish.ToString();
    }

    public override async Task<ContractParamDto> AdaptToContractParamAsync<TDropFinishBizDto>(TDropFinishBizDto invokeBizDto)
    {
        _logger.Debug("NFTDrop Finish AdaptToContractParamAsync");
        AssertHelper.NotNull(invokeBizDto, "DropFinish empty");
        AssertHelper.NotNull(invokeBizDto is NFTDropFinishBizDto, "Invalid DropFinishBizDto type");
        
        var dropFinishDto = invokeBizDto as NFTDropFinishBizDto;
        AssertHelper.NotNull(dropFinishDto, "DropFinishBizDto empty");

        var contractParamDto = new ContractParamDto
        {
            BizId = dropFinishDto?.DropId,
            BizType = BizType(),
            ChainId = DealerContractType.SideChainId,
            ContractName = DealerContractType.DropContractName,
            ContractMethod = DealerContractType.DropFinishMethod,
            Sender = DealerContractType.DropFinishMethod,
            BizData = new FinishDropInput()
            {
                DropId = Hash.LoadFromHex(dropFinishDto?.DropId),
                // TODO 填入实际index
                Index = 1
            }.ToByteString().ToBase64()
        };
        return contractParamDto;
    }

    public override async Task ResultCallbackAsync(string bizId, bool invokeSuccess, TransactionResultDto result,
        string rawTransaction)
    {
        _logger.Debug("NFTDrop Finish ResultCallbackAsync");
        return;
    }
}