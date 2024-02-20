using System.Collections.Generic;
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
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Provider;
using Orleans.Runtime;

namespace NFTMarketServer.Dealer.ContractInvoker;

public class NFTDropFinishInvoker : AbstractContractInvoker
{
    private readonly ILogger<NFTDropFinishInvoker> _logger;
    private readonly IOptionsMonitor<ForestChainOptions> _optionsMonitor;

    public NFTDropFinishInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, 
        IObjectMapper objectMapper,
        ILogger<NFTDropFinishInvoker> logger,
        IOptionsMonitor<ForestChainOptions> optionsMonitor) : base(distributedEventBus,
        contractInvokeProvider, objectMapper)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    public override string BizType()
    {
        return Dtos.BizType.NFTDropFinish.ToString();
    }

    public override async Task<ContractParamDto> AdaptToContractParamAsync<TDropFinishBizDto>(TDropFinishBizDto invokeBizDto)
    {
        _logger.Debug("NFTDrop Finish AdaptToContractParamAsync begin");
        AssertHelper.NotNull(invokeBizDto, "DropFinish empty");
        AssertHelper.NotNull(invokeBizDto is NFTDropFinishBizDto, "Invalid DropFinishBizDto type");
        
        var dropFinishDto = invokeBizDto as NFTDropFinishBizDto;
        AssertHelper.NotNull(dropFinishDto, "DropFinishBizDto empty");

        var chainList = _optionsMonitor.CurrentValue.Chains;
        if (chainList.IsNullOrEmpty() || chainList.Count < 2)
        {
            _logger.LogInformation("invalid chain list");
        }
        
        
        var contractParamDto = new ContractParamDto
        {
            BizId = dropFinishDto?.DropId,
            BizType = BizType(),
            ChainId = chainList[1],
            ContractName = DealerContractType.DropContractName,
            ContractMethod = DealerContractType.DropFinishMethod,
            Sender = DealerContractType.DropFinishAccount,
            BizData = new FinishDropInput()
            {
                DropId = Hash.LoadFromHex(dropFinishDto?.DropId),
                Index = dropFinishDto.Index
            }.ToByteString().ToBase64()
        };
        
        _logger.Debug("NFTDrop Finish AdaptToContractParamAsync end, param: {data}", JsonConvert.SerializeObject(contractParamDto));
        return contractParamDto;
    }

    public override async Task ResultCallbackAsync(string bizId, bool invokeSuccess, TransactionResultDto result,
        string rawTransaction)
    {
        _logger.Debug($"NFTDrop Finish ResultCallbackAsync， result: {result}", JsonConvert.SerializeObject(result));
        return;
    }
}