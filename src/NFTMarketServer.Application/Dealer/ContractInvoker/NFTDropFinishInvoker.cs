using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Types;
using Forest.Contracts.Drop;
using Google.Protobuf;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Provider;
using NFTMarketServer.Chains;
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
    private readonly IChainAppService _chainAppService;

    public NFTDropFinishInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, 
        IObjectMapper objectMapper,
        ILogger<NFTDropFinishInvoker> logger,
        IChainAppService chainAppService) : base(distributedEventBus,
        contractInvokeProvider, objectMapper)
    {
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _logger = logger;
        _chainAppService = chainAppService;
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

        var sideChainId = await _chainAppService.GetChainIdAsync(1);
        var contractParamDto = new ContractParamDto
        {
            BizId = dropFinishDto?.DropId,
            BizType = BizType(),
            ChainId = sideChainId,
            ContractName = DealerContractType.DropContractName,
            ContractMethod = DealerContractType.DropFinishMethod,
            Sender = DealerContractType.DropFinishAccount,
            BizData = new FinishDropInput()
            {
                DropId = Hash.LoadFromHex(dropFinishDto?.DropId),
                Index = dropFinishDto.Index
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