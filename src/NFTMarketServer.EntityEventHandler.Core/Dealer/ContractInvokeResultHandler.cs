using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Common;
using NFTMarketServer.Contracts.HandleException;
using NFTMarketServer.Dealer.ContractInvoker;
using NFTMarketServer.Dealer.Etos;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.EntityEventHandler.Core.Dealer;

public class ContractInvokeResultHandler : IDistributedEventHandler<ContractInvokeResultEto>, ITransientDependency
{
    private readonly ILogger<ContractInvokeResultHandler> _logger;
    private readonly IContractInvokerFactory _contractInvokerFactory;

    public ContractInvokeResultHandler(IContractInvokerFactory contractInvokerFactory,
        ILogger<ContractInvokeResultHandler> logger)
    {
        _contractInvokerFactory = contractInvokerFactory;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ContractInvokeResultHandler.HandleEventAsync Dealer contract invoke result handle failed", 
        TargetType = typeof(ExceptionHandlingService), 
        LogOnly = true,
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new []{"eventData" }
    )]
    public virtual async Task HandleEventAsync(ContractInvokeResultEto eventData)
    {
        AssertHelper.NotNull(eventData.TransactionResult, "transactionResult missing");
        AssertHelper.NotEmpty(eventData.BizId, "bizId missing");
        AssertHelper.NotEmpty(eventData.BizType, "BizType missing");
            
        _logger.LogInformation(
            "Dealer contract invoke result received, bizType={Type}, bizId={Id}, txId={TxId}, status={Status}",
            eventData.BizType, eventData.BizId, eventData.TransactionResult.TransactionId,
            eventData.TransactionResult.Status);

        await _contractInvokerFactory.Invoker(eventData.BizType).ResultCallbackAsync(
            eventData.BizId,
            eventData.TransactionResult.Status.Equals(TransactionState.Mined),
            eventData.TransactionResult, eventData.RawTransaction);
    }
}