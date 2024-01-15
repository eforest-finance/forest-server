using System;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Dealer.Provider;

public class CrossChainProvider : ISingletonDependency
{
    private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly IOptionsMonitor<SynchronizeTransactionJobOptions> _optionsMonitor;
    private readonly ILogger<CrossChainProvider> _logger;
    private readonly string _defaultMainChain = "AELF";
    public CrossChainProvider(
        IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        ILogger<CrossChainProvider> logger,
        IOptionsMonitor<SynchronizeTransactionJobOptions> optionsMonitor)
    {
        _blockchainClientFactory = blockchainClientFactory;
        _chainOptionsMonitor = chainOptionsMonitor;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task WaitingHeightAsync(long expectHeight)
    {
        var indexHeight = await GetIndexHeightAsync(GetDefaultSideChainId());
        while (indexHeight <= expectHeight)
        {
            await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
            indexHeight = await GetIndexHeightAsync(GetDefaultSideChainId());
        }
    }

    private async Task<long> GetIndexHeightAsync(string chainId)
    {
        var chainInfo = _chainOptionsMonitor.CurrentValue.ChainInfos[chainId];

        var client = _blockchainClientFactory.GetClient(chainId);
        try
        {
            var transaction = await client.GenerateTransactionAsync(
                client.GetAddressFromPrivateKey(chainInfo.PrivateKey),
                chainInfo.CrossChainContractAddress, MethodName.GetParentChainHeight, new Empty());
            var txWithSign = client.SignTransaction(chainInfo.PrivateKey, transaction);

            var transactionGetTokenResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });

            var result = Int64Value.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionGetTokenResult));
            return result.Value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetIndexBlockHeight on chain {Id} error", chainId);
            await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
        }

        return 0;
    }

    public async Task<MerklePathDto> GetMerklePathDtoAsync(string chainId, string transactionId)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
                return await _blockchainClientFactory.GetClient(chainId)
                    .GetMerklePathByTransactionIdAsync(transactionId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetMerklePathDtoAsync on TransactionId {TransactionId} count {i} error",
                    transactionId, i);
                await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
            }
        }

        return null;

    }

    public async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                return await _blockchainClientFactory.GetClient(chainId)
                    .GetTransactionResultAsync(transactionId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetTransactionResultAsync on TransactionId {TransactionId} error {i}",
                    transactionId, i);
                await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
            }
        }

        return null;

    }

    private string GetDefaultSideChainId()
    {
        var chainIds = _chainOptionsMonitor.CurrentValue.ChainInfos.Keys;
        foreach (var chainId in chainIds)
        {
            if (!chainId.Equals(_defaultMainChain))
            {
                return chainId;
            }
        }

        return _defaultMainChain;
    }
}