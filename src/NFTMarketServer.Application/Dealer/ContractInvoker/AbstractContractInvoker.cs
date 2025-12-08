using System;
using System.Security.AccessControl;
using System.Threading.Tasks;
using AElf.Client.Dto;
using Newtonsoft.Json;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Etos;
using NFTMarketServer.Dealer.Provider;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Dealer.ContractInvoker;

public interface IContractInvoker
{
    string BizType();
    Task InvokeAsync<TBizData>(TBizData invokeBizDto);

    Task ResultCallbackAsync(string bizId, bool invokeSuccess, TransactionResultDto result,
        string rawTransaction);
    Task<TransactionResultDto> GetTransactionResultAsync(string bizId);
}

public abstract class AbstractContractInvoker : IContractInvoker
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ContractInvokeProvider _contractInvokeProvider;
    private readonly IObjectMapper _objectMapper;


    protected AbstractContractInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, IObjectMapper objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _contractInvokeProvider = contractInvokeProvider;
        _objectMapper = objectMapper;
    }

    /// <summary>
    ///     BizType of Invoker
    /// </summary>
    /// <see cref="Dtos.BizType"/>
    /// <returns></returns>
    public abstract string BizType();

    /// <summary>
    ///     Adapt bizData to ContractParam
    /// </summary>
    /// <param name="invokeBizDto"></param>
    /// <typeparam name="TBizData"></typeparam>
    /// <returns></returns>
    public abstract Task<ContractParamDto> AdaptToContractParamAsync<TBizData>(TBizData invokeBizDto);

    /// <summary>
    ///     after transaction sent, publish custom event back to biz logic
    /// </summary>
    /// <param name="bizId"></param>
    /// <param name="invokeSuccess"></param>
    /// <param name="result"></param>
    /// <param name="rawTransaction"></param>
    /// <returns></returns>
    public virtual Task ResultCallbackAsync(string bizId, bool invokeSuccess, TransactionResultDto result,
        string rawTransaction)
    {
        // do nothing as default
        return Task.CompletedTask;
    }

    public async Task InvokeAsync<TBizData>(TBizData invokeBizDto)
    {
        // adapt bizParam to invoke-contract-param
        var contractParamDto = await AdaptToContractParamAsync(invokeBizDto);
        // save Grain
        await _contractInvokeProvider.CreateAsync(contractParamDto);
        // publish to invoke
        await _distributedEventBus.PublishAsync(new ContractInvokeEto
        {
            ContractParamDto = contractParamDto
        });
    }

    public async Task<TransactionResultDto> GetTransactionResultAsync(string bizId)
    {
        var grainDto = await _contractInvokeProvider.GetByIdAsync(BizType(), bizId);
        return grainDto.TransactionResult.IsNullOrEmpty()
            ? null
            : JsonConvert.DeserializeObject<TransactionResultDto>(grainDto.TransactionResult);
    }
}