using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.ExceptionHandler;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Etos;
using NFTMarketServer.Dealer.Index;
using NFTMarketServer.Dealer.Options;
using NFTMarketServer.Dealer.Provider;
using NFTMarketServer.HandleException;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using TransactionResultStatus = NFTMarketServer.Dealer.Dtos.TransactionResultStatus;

namespace NFTMarketServer.Dealer.Worker;

public interface IContractInvokerWorker
{
    Task Invoke();
}

public class ContractInvokerWorker : IContractInvokerWorker, ISingletonDependency
{
    private const string LockKeyPrefix = "ContractInvokerProvider:";

    private readonly ILogger<ContractInvokerWorker> _logger;
    private readonly IAbpDistributedLock _distributedLock;

    private readonly IOptionsMonitor<ChainOption> _chainOption;
    private readonly ContractInvokeProvider _contractInvokeProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IDistributedEventBus _distributedEventBus;

    public ContractInvokerWorker(
        ILogger<ContractInvokerWorker> logger,
        IAbpDistributedLock distributedLock,
        IContractProvider contractProvider,
        ContractInvokeProvider contractInvokeProvider, IDistributedEventBus distributedEventBus,
        IOptionsMonitor<ChainOption> chainOption)
    {
        _logger = logger;
        _distributedLock = distributedLock;
        _contractProvider = contractProvider;
        _contractInvokeProvider = contractInvokeProvider;
        _distributedEventBus = distributedEventBus;
        _chainOption = chainOption;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("ContractInvokerWorker start...");
        await using var handle = await _distributedLock.TryAcquireAsync(name: LockKeyPrefix);
        if (handle == null)
        {
            _logger.LogInformation("ContractInvokerWorker still running, skipped");
            return;
        }

        var pageSize = 100;
        var updateTimeLt = DateTime.UtcNow.AddSeconds(- _chainOption.CurrentValue.QueryPendingTxSecondsAgo).ToUtcMilliSeconds().ToString();
        var count = 0;
        while (true)
        {
            var pendingList = await _contractInvokeProvider.QueryPendingResult(updateTimeLt, pageSize);
            if (pendingList.IsNullOrEmpty()) break;
            updateTimeLt = pendingList.Min(i => i.UpdateTime);
            foreach (var index in pendingList)
            {
                await UpdateResult(index);
                count++;
            }
        }

        _logger.LogInformation("ContractInvokerWorker finish, total : {Count}", count);
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ContractInvokerWorker.UpdateResult ContractInvokerProvider updateResult error,", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new []{"contractInvokeIndex"}
    )]
    public virtual async Task UpdateResult(ContractInvokeIndex contractInvokeIndex)
    {
        AssertHelper.NotEmpty(contractInvokeIndex.TransactionId, "transactionId empty");
        var transactionId = Hash.LoadFromHex(contractInvokeIndex.TransactionId);
        var rawTxResult =
            await _contractProvider.QueryTransactionResultAsync(contractInvokeIndex.ChainId, transactionId);
        if (rawTxResult == null || rawTxResult.Status == contractInvokeIndex.Status)
        {
            return;
        }

        var grainDto =
            await _contractInvokeProvider.GetByIdAsync(contractInvokeIndex.BizType, contractInvokeIndex.BizId);

        if (rawTxResult.Status == TransactionResultStatus.PENDING.ToString())
        {
            // waiting
            return;
        }

        // SaveTxResultAsync can be time consuming and No need to wait for the end.
        _ = SaveTxResultAsync(contractInvokeIndex, grainDto, rawTxResult);
    }

    private async Task SaveTxResultAsync(ContractInvokeIndex contractInvokeIndex,
        ContractInvokeGrainDto grainDto, TransactionResultDto rawTxResult)
    {
        // update result
        var txResultJson = JsonConvert.SerializeObject(rawTxResult);

        grainDto.UpdateTime = DateTime.UtcNow.ToUtcString();
        grainDto.TransactionStatus = rawTxResult.Status;
        grainDto.TransactionResult = txResultJson;
        grainDto.TransactionStatusFlow.Add(new TransactionStatus
        {
            TimeStamp = DateTime.UtcNow.ToUtcString(),
            Status = rawTxResult.Status,
            TransactionResult = rawTxResult.Status == TransactionResultStatus.MINED.ToString() ? null : txResultJson
        });

        grainDto.Status = rawTxResult.Status == TransactionResultStatus.MINED.ToString()
            ? ContractInvokeSendStatus.Success.ToString()
            : ContractInvokeSendStatus.Failed.ToString();
        await _contractInvokeProvider.AddUpdateAsync(grainDto);

        // invoke callback for biz
        await _distributedEventBus.PublishAsync(new ContractInvokeResultEto
        {
            BizType = grainDto.BizType,
            BizId = grainDto.BizId,
            TransactionResult = rawTxResult,
            RawTransaction = contractInvokeIndex.RawTransaction
        });
    }
}