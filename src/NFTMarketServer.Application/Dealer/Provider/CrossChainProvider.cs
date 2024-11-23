using System;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.ExceptionHandler;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.HandleException;
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
        var indexHeight = await GetIndexHeightAsync(GetDefaultSideChainId(),_optionsMonitor.CurrentValue.CrossChainDelay);
        while (indexHeight <= expectHeight)
        {
            await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
            indexHeight = await GetIndexHeightAsync(GetDefaultSideChainId(), _optionsMonitor.CurrentValue.CrossChainDelay);
        }
    }
    [ExceptionHandler(typeof(Exception),
        Message = "CrossChainProvider.GetIndexBlockHeight on chain is fail", 
        TargetType = typeof(CrossChainExceptionHandlingService), 
        MethodName = nameof(CrossChainExceptionHandlingService.HandleExceptionDelayReturn),
        LogTargets = new []{"chainId", "delayTime"})]
    public virtual async Task<long> GetIndexHeightAsync(string chainId, int delayTime)
    {
        var chainInfo = _chainOptionsMonitor.CurrentValue.ChainInfos[chainId];

        var client = _blockchainClientFactory.GetClient(chainId);
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
    [ExceptionHandler(typeof(Exception),
        Message = "CrossChainProvider.GetMerklePathDtoAsync on TransactionId is fail", 
        TargetType = typeof(CrossChainExceptionHandlingService), 
        MethodName = nameof(CrossChainExceptionHandlingService.HandleExceptionDelayDefaultReturn),
        LogTargets = new []{"chainId", "transactionId"})]
    public virtual async Task<MerklePathDto> GetMerklePathDtoAsync(string chainId, string transactionId)
    {
        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
            return await _blockchainClientFactory.GetClient(chainId)
                .GetMerklePathByTransactionIdAsync(transactionId);
        }
        return null;
    }
    [ExceptionHandler(typeof(Exception),
        Message = "CrossChainProvider.GetTransactionResultAsync on TransactionId is fail", 
        TargetType = typeof(CrossChainExceptionHandlingService), 
        MethodName = nameof(CrossChainExceptionHandlingService.HandleExceptionDelayDefaultReturn),
        LogTargets = new []{"chainId", "transactionId"})]
    public virtual async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
    {
        for (int i = 0; i < 3; i++)
        {
            return await _blockchainClientFactory.GetClient(chainId)
                .GetTransactionResultAsync(transactionId);
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