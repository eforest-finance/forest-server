using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Helper;
using NFTMarketServer.Dealer.Options;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Dealer.Provider;

public interface IContractProvider
{
    Hash CreateTransaction(string chainId, string senderName, string contractName, string methodName,
        ByteString param, out Transaction transaction);

    Task SendTransactionAsync(string chainId, Transaction transaction);

    Task<TransactionResultDto> QueryTransactionResultAsync(string chainId, Hash transactionId);

    public Task<T> CallTransactionAsync<T>(string chainId, Transaction transaction) where T : class, IMessage<T>, new();

    public Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId);

}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, SenderAccount> _accounts = new();
    private readonly Dictionary<string, string> _emptyDict = new();

    private readonly IOptionsMonitor<ChainOption> _chainOption;
    private readonly IAelfClientProvider _aelfClientProvider;
    private readonly ILogger<ContractProvider> _logger;

    public ContractProvider(
        IOptionsMonitor<ChainOption> chainOption,
        ILogger<ContractProvider> logger, IAelfClientProvider aelfClientProvider)
    {
        _chainOption = chainOption;
        _logger = logger;
        _aelfClientProvider = aelfClientProvider;
    }


    private SenderAccount InitAccount(string accountName)
    {
        var optionExists = _chainOption.CurrentValue.AccountOption.TryGetValue(accountName, out var accountOption);
        AssertHelper.NotNull(optionExists, "Account {Name} not found", accountName);
        AssertHelper.NotEmpty(accountOption?.PrivateKey, "Private key of account {name} not found", accountName);
        return new SenderAccount(accountOption?.PrivateKey);
    }

    private string ContractAddress(string chainId, string contractName)
    {
        var contractAddress = _chainOption.CurrentValue.ContractAddress
            .GetValueOrDefault(chainId, _emptyDict)
            .GetValueOrDefault(contractName, null);
        AssertHelper.NotNull(contractAddress, "Address of contract {contractName} on {chainId} not exits.",
            contractName, chainId);
        return contractAddress;
    }

    private SenderAccount Account(string accountName)
    {
        return _accounts.GetOrAdd(accountName, InitAccount);
    }

    public Hash CreateTransaction(string chainId, string senderName, string contractName, string methodName,
        ByteString param, out Transaction transaction)
    {
        var address = ContractAddress(chainId, contractName);
        ChainStatusDto status;
        try
        {
            status = _aelfClientProvider.GetChainStatusAsync(chainId).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetChainStatusAsync chainId error {ChainId} ", chainId);
            transaction = null;
            return null;
        }
        var height = status.BestChainHeight;
        var blockHash = status.BestChainHash;
        var account = Account(senderName);

        // create raw transaction
        transaction = new Transaction
        {
            From = account.Address,
            To = Address.FromBase58(address),
            MethodName = methodName,
            Params = param,
            RefBlockNumber = height,
            RefBlockPrefix = ByteString.CopyFrom(Hash.LoadFromHex(blockHash).Value.Take(4).ToArray())
        };

        var transactionId = HashHelper.ComputeFrom(transaction.ToByteArray());
        transaction.Signature = account.GetSignatureWith(transactionId.ToByteArray());
        return transactionId;
    }

    public  async Task SendTransactionAsync(string chainId, Transaction transaction)
    {
        try
        {
            _logger.LogDebug(
                "Send transaction on chain {ChainId}, transactionId:{TransactionId}", chainId, transaction.GetHash());
            var sendResult = await _aelfClientProvider.SendTransactionAsync(chainId, new SendTransactionInput()
            {
                RawTransaction = transaction.ToByteArray().ToHex()
            });
            _logger.LogDebug(
                "Send transaction on chain {ChainId}, transactionId:{TransactionId}, transaction: {Transaction}",
                chainId, sendResult.TransactionId, transaction.ToByteArray().ToHex());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Send transaction failed on chain {ChainId}, transactionId:{TransactionId}",
                chainId, transaction.GetHash());
        }
      
    }
    
    

    public async Task<TransactionResultDto> QueryTransactionResultAsync(string chainId, Hash transactionId)
    {
        try
        {
            _logger.LogDebug(
                "Send transaction on chain {ChainId}, transactionId:{TransactionId}", chainId, transactionId.ToHex());
            var txResult = await _aelfClientProvider.GetTransactionResultAsync(chainId, transactionId.ToHex());

            _logger.LogDebug(
                "Query transaction on chain {ChainId}, transactionId:{TransactionId}, status: {Status}",
                chainId, transactionId.ToHex(), txResult.Status);

            return txResult;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTransactionResultAsync failed on chain {ChainId}, transactionId:{TransactionId}",
                chainId, transactionId.ToHex());
        }

        return null;
    }

    public async Task<T> CallTransactionAsync<T>(string chainId, Transaction transaction)
        where T : class, IMessage<T>, new()
    {
        return await _aelfClientProvider.CallTransactionAsync<T>(chainId, new SendTransactionInput()
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });
    }

    public Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
    {
        return _aelfClientProvider.GetMerklePathAsync(chainId, txId);
    }


}