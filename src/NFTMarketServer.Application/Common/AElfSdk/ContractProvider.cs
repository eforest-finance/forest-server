using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NFTMarketServer.Common.AElfSdk.Dtos;
using NFTMarketServer.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace NFTMarketServer.Common.AElfSdk;

public interface IContractProvider
{
    Task<(Hash transactionId, Transaction transaction)> CreateTransaction(string chainId, string senderName,
        string contractName, string methodName,
        IMessage param);

    Task<SendTransactionOutput> SendTransactionAsync(string chainId, Transaction transaction);

    Task<T> CallTransactionAsync<T>(string chainId, Transaction transaction) where T : class;

    Task<TransactionResultDto> QueryTransactionResult(string transactionId, string chainId);
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
    {
        Converters = new List<JsonConverter> { new AddressConverter() }
    };

    private readonly Dictionary<string, AElfClient> _clients = new();
    private readonly Dictionary<string, SenderAccount> _accounts = new();
    private readonly Dictionary<string, string> _emptyDict = new();
    private readonly Dictionary<string, Dictionary<string, string>> _contractAddress = new();

    private readonly ILogger<ContractProvider> _logger;
    private readonly IOptionsMonitor<ChainOption> _chainOption;
    public ContractProvider(IOptionsMonitor<ChainOption> chainOption, ILogger<ContractProvider> logger)
    {
        _logger = logger;
        _chainOption = chainOption;
        InitAElfClient();
    }


    private void InitAElfClient()
    {
        if (_chainOption.CurrentValue.NodeApis.IsNullOrEmpty())
        {
            return;
        }

        foreach (var node in _chainOption.CurrentValue.NodeApis)
        {
            _clients[node.Key] = new AElfClient(node.Value);
            _logger.LogInformation("init AElfClient: {ChainId}, {Node}", node.Key, node.Value);
        }
    }

    private AElfClient Client(string chainId)
    {
        AssertHelper.IsTrue(_clients.ContainsKey(chainId), "AElfClient of {chainId} not found.config={B}", chainId,
            JsonConvert.SerializeObject(_chainOption.CurrentValue));
        return _clients[chainId];
    }

    private SenderAccount Account(string accountName)
    {
        return _accounts.GetOrAdd(accountName, name =>
        {
            var accountExists = _chainOption.CurrentValue.AccountPrivateKey.TryGetValue(name, out var accountOption);
            AssertHelper.IsTrue(accountExists, "Account {Name} not exists", name);
            AssertHelper.NotEmpty(accountOption, "Account {Name} not found", name);
            var account = new SenderAccount(accountOption);
            _logger.LogInformation("init Sender Account: {Name}, {Address}", name, account.Address.ToBase58());
            return account;
        });
    }

    private string ContractAddress(string chainId, string contractName)
    {
        var contractAddress = _contractAddress.GetOrAdd(chainId, _ => new Dictionary<string, string>());
        return contractAddress.GetOrAdd(contractName, name =>
        {
            var address = _chainOption.CurrentValue.ContractAddress
                .GetValueOrDefault(chainId, _emptyDict)
                .GetValueOrDefault(name, null);
            if (CollectionUtilities.IsNullOrEmpty(address) && SystemContractName.All.Contains(name))
                address = AsyncHelper
                    .RunSync(() => Client(chainId).GetContractAddressByNameAsync(HashHelper.ComputeFrom(name)))
                    .ToBase58();

            AssertHelper.NotEmpty(address, "Address of contract {contractName} on {chainId} not exits.",
                name, chainId);
            return address;
        });
    }

    public Task<TransactionResultDto> QueryTransactionResult(string transactionId, string chainId)
    {
        return Client(chainId).GetTransactionResultAsync(transactionId);
    }

    public async Task<(Hash transactionId, Transaction transaction)> CreateTransaction(string chainId,
        string senderName, string contractName, string methodName,
        IMessage param)
    {
        var address = ContractAddress(chainId, contractName);
        var client = Client(chainId);
        var status = await client.GetChainStatusAsync();
        var height = status.BestChainHeight;
        var blockHash = status.BestChainHash;
        var account = Account(senderName);

        // create raw transaction
        var transaction = new Transaction
        {
            From = account.Address,
            To = Address.FromBase58(address),
            MethodName = methodName,
            Params = param.ToByteString(),
            RefBlockNumber = height,
            RefBlockPrefix = ByteString.CopyFrom(Hash.LoadFromHex(blockHash).Value.Take(4).ToArray())
        };

        var transactionId = HashHelper.ComputeFrom(transaction.ToByteArray());
        transaction.Signature = account.GetSignatureWith(transactionId.ToByteArray());
        return (transactionId, transaction);
    }

    public async Task<SendTransactionOutput> SendTransactionAsync(string chainId, Transaction transaction)
    {
        var client = Client(chainId);
        return await client.SendTransactionAsync(new SendTransactionInput()
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });
    }

    public async Task<T> CallTransactionAsync<T>(string chainId, Transaction transaction) where T : class
    {
        var client = Client(chainId);
        var rawTransactionResult = await client.ExecuteRawTransactionAsync(new ExecuteRawTransactionDto()
        {
            RawTransaction = transaction.ToByteArray().ToHex(),
            Signature = transaction.Signature.ToHex()
        });

        if (typeof(T) == typeof(string))
        {
            return rawTransactionResult?.Substring(1, rawTransactionResult.Length - 2) as T;
        }

        return (T)JsonConvert.DeserializeObject(rawTransactionResult, typeof(T), _settings);
    }
}

public class AddressConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Address);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return Address.FromBase58(reader.Value as string);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}