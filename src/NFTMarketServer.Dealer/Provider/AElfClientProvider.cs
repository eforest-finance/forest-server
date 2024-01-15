using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Options;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Dealer.Provider;

public interface IAelfClientProvider
{
    Task<ChainStatusDto> GetChainStatusAsync(string chainId);
    Task<SendTransactionOutput> SendTransactionAsync(string chainId, SendTransactionInput input);
    Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId);

    Task<T> CallTransactionAsync<T>(string chainId, SendTransactionInput input) where T : class, IMessage<T>, new();

    public Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId);
}

public class AelfClientProvider: IAelfClientProvider, ISingletonDependency
{
    private readonly Dictionary<string, AElfClient> _clients = new();
    private readonly IOptionsMonitor<ChainOption> _chainOption;
    private readonly ILogger<AelfClientProvider> _logger;

    public AelfClientProvider(IOptionsMonitor<ChainOption> chainOption, ILogger<AelfClientProvider> logger)
    {
        _chainOption = chainOption;
        _logger = logger;
        InitAElfClient();
    }
    
    private void InitAElfClient()
    {
        if (_chainOption.CurrentValue.ChainNode.IsNullOrEmpty())
        {
            return;
        }
        foreach (var node in _chainOption.CurrentValue.ChainNode)
        {
            _clients[node.Key] = new AElfClient(node.Value);
            _logger.LogDebug("init AElfClient: {ChainId}, {Node}", node.Key, node.Value);
        }
    }
    
    private AElfClient Client(string chainId)
    {
        AssertHelper.IsTrue(_clients.ContainsKey(chainId), "AElfClient of {chainId} not found.", chainId);
        return _clients[chainId];
    }


    public async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
    {
        return await Client(chainId).GetChainStatusAsync();
    }

    public async Task<SendTransactionOutput> SendTransactionAsync(string chainId, SendTransactionInput input)
    {
        return await Client(chainId).SendTransactionAsync(input);
    }
    
    public async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        return await Client(chainId).GetTransactionResultAsync(transactionId);
    }

    public async Task<T> CallTransactionAsync<T>(string chainId, SendTransactionInput input)
        where T : class, IMessage<T>, new()
    {
        var result = await Client(chainId)
            .ExecuteTransactionAsync(new ExecuteTransactionDto() { RawTransaction = input.RawTransaction });
        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));
        return value;
    }

    public async Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
    {
        try
        {
            return await Client(chainId).GetMerklePathByTransactionIdAsync(txId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{chainId}-{txId} There was an error getting the merkle path, try again later", chainId,
                txId);
            return null;
        }
    }
    
    
}