using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Etos;
using NFTMarketServer.Dealer.Options;
using NFTMarketServer.Dealer.Provider;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using TransactionResultStatus = NFTMarketServer.Dealer.Dtos.TransactionResultStatus;

namespace NFTMarketServer.Dealer.Handler;

public class ContractInvokeHandler : IDistributedEventHandler<ContractInvokeEto>, ISingletonDependency
{
    private readonly ILogger<ContractInvokeHandler> _logger;
    private readonly IOptionsMonitor<ChainOption> _chainOption;
    private readonly ContractInvokeProvider _contractInvokeProvider;
    private readonly IContractProvider _contractProvider;
    private readonly IDistributedEventBus _distributedEventBus;

    public ContractInvokeHandler(IOptionsMonitor<ChainOption> chainOption, ILogger<ContractInvokeHandler> logger,
        ContractInvokeProvider contractInvokeProvider, IContractProvider contractProvider,
        IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _contractInvokeProvider = contractInvokeProvider;
        _contractProvider = contractProvider;
        _distributedEventBus = distributedEventBus;
        _chainOption = chainOption;
    }

    public async Task HandleEventAsync(ContractInvokeEto eventData)
    {
        var contractParam = eventData.ContractParamDto;
        var count = 0;
        try
        {
            Hash transactionId;
            Transaction transaction;
            int RepeatCount = 0;
            do
            {
                RepeatCount++;
                transactionId = _contractProvider.CreateTransaction(contractParam.ChainId, contractParam.Sender,
                    contractParam.ContractName, contractParam.ContractMethod,
                    ByteString.FromBase64(contractParam.BizData),
                    out transaction);
                await Task.Delay(_chainOption.CurrentValue.QueryTransactionDelayMillis);
            } while (transactionId == null && RepeatCount <= 5);

            if (transactionId == null)
            {
                _logger.LogError("invoke contract CreateTransaction error {BizType}_{BizId} ERROR count {Count}",
                    eventData.ContractParamDto.BizType, eventData.ContractParamDto.BizId, count);
            }

            var grainDto = await _contractInvokeProvider.GetByIdAsync(contractParam.BizType, contractParam.BizId);
            grainDto.RawTransaction = transaction.ToByteString().ToBase64();
            grainDto.TransactionId = transactionId.ToHex();
            grainDto.ExecutionCount += 1;
            count = grainDto.ExecutionCount;
            if (grainDto.ExecutionCount > 5)
            {
                return;
            }
            await _contractInvokeProvider.AddUpdateAsync(grainDto);

            await _contractProvider.SendTransactionAsync(contractParam.ChainId, transaction);

            await UpdateResultAsync(contractParam.ChainId, transactionId, grainDto);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "invoke contract for {BizType}_{BizId} FAILED count {Count}",
                eventData.ContractParamDto.BizType, eventData.ContractParamDto.BizId, count);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "invoke contract for {BizType}_{BizId} ERROR count {Count}",
                eventData.ContractParamDto.BizType, eventData.ContractParamDto.BizId, count);
        }
    }


    private async Task UpdateResultAsync(string chainId, Hash transactionId, ContractInvokeGrainDto grainDto)
    {
        var cts = new CancellationTokenSource(_chainOption.CurrentValue.InvokeExpireSeconds * 1000);
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var rawTxResult = await _contractProvider.QueryTransactionResultAsync(chainId, transactionId);
                if (rawTxResult == null)
                {
                    _logger.LogDebug(
                        "ContractInvoke result callback error, bizType={BizType}, bizId={BizId}, txId={TxId},  ExecutionCount = {ExecutionCount}",
                        grainDto.BizType, grainDto.BizId, transactionId,
                        grainDto.ExecutionCount);
                    await _distributedEventBus.PublishAsync(new ContractInvokeResultEto
                    {
                        BizType = grainDto.BizType,
                        BizId = grainDto.BizId,
                        TransactionResult = new TransactionResultDto
                        {
                            TransactionId = transactionId.ToHex(),
                            Status = TransactionResultStatus.FAILED.ToString(),
                            Error = "unknown"
                        },
                        RawTransaction = grainDto.RawTransaction
                    });
                    return;
                }

                if (grainDto.TransactionStatus.IsNullOrEmpty() || grainDto.TransactionStatus != rawTxResult.Status)
                {
                    var txResultJson = JsonConvert.SerializeObject(rawTxResult);
                    grainDto.TransactionStatusFlow.Add(new TransactionStatus
                    {
                        TimeStamp = DateTime.UtcNow.ToUtcString(),
                        Status = rawTxResult.Status,
                        TransactionResult = rawTxResult.Status == TransactionResultStatus.PENDING.ToString() 
                                            || rawTxResult.Status == TransactionResultStatus.MINED.ToString()
                            ? null
                            : txResultJson
                    });
                    grainDto.TransactionResult = txResultJson;
                    grainDto.TransactionStatus = rawTxResult.Status;
                    grainDto.Status = grainDto.TransactionStatus == TransactionResultStatus.MINED.ToString()
                        ? ContractInvokeSendStatus.Success.ToString()
                        : ContractInvokeSendStatus.Sent.ToString();
                    _logger.LogDebug(
                        "ContractInvoke result update, bizType={BizType}, bizId={BizId}, txId={TxId}, status={TxStatus}",
                        grainDto.BizType, grainDto.BizId, transactionId, rawTxResult.Status);
                    await _contractInvokeProvider.AddUpdateAsync(grainDto);
                }

                if (rawTxResult.Status != TransactionResultStatus.NOTEXISTED.ToString() &&
                    rawTxResult.Status != TransactionResultStatus.PENDING.ToString())
                {
                    _logger.LogDebug(
                        "ContractInvoke result callback, bizType={BizType}, bizId={BizId}, txId={TxId}, status={TxStatus} error = {Error},ExecutionCount = {ExecutionCount}",
                        grainDto.BizType, grainDto.BizId, transactionId, rawTxResult.Status, rawTxResult.Error,
                        grainDto.ExecutionCount);
                    await _distributedEventBus.PublishAsync(new ContractInvokeResultEto
                    {
                        BizType = grainDto.BizType,
                        BizId = grainDto.BizId,
                        TransactionResult = rawTxResult,
                        RawTransaction = grainDto.RawTransaction
                    });
                    break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Timed out waiting for transactionId {TransactionId} result", transactionId);
                break;
            }

            // delay some times
            await Task.Delay(_chainOption.CurrentValue.QueryTransactionDelayMillis, cts.Token);
        }

        cts.Cancel();
    }
}