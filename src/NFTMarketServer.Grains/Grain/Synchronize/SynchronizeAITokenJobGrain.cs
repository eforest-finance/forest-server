using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.ExceptionHandler;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Contracts.HandleException;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.State.Synchronize;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.Synchronize.Ai;

public class SynchronizeAITokenJobGrain : Grain<SynchronizeAITokenState>, ISynchronizeAITokenJobGrain
{
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly ILogger<SynchronizeAITokenJobGrain> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;

    private const string DefaultFromChain = "AELF";


    public SynchronizeAITokenJobGrain(ILogger<SynchronizeAITokenJobGrain> logger,
        IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        IObjectMapper objectMapper, IBlockchainClientFactory<AElfClient> blockchainClientFactory)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _blockchainClientFactory = blockchainClientFactory;
        _chainOptionsMonitor = chainOptionsMonitor;
    }

    public async Task<GrainResultDto<SynchronizeAITokenJobGrainDto>> CreateSynchronizeAITokenJobAsync(
        SaveSynchronizeAITokenJobGrainDto input)
    {
        if (State == null || State.Id.IsNullOrEmpty())
        {
            State = new SynchronizeAITokenState()
            {
                Id = input.Symbol,
                Symbol = input.Symbol,
                Status = CrossCreateAITokenStatus.TokenCreating,
                CreateTime = DateTime.UtcNow.Second,
                UpdateTime = DateTime.UtcNow.Second,
                FromChainId = input.FromChainId,
                ToChainId = input.ToChainId
            };
        }
        else
        {
            State.Status = input.Status;
            State.Message = input.Message;
            State.UpdateTime = DateTime.UtcNow.Second;
        }

        await WriteStateAsync();
        _logger.LogInformation("{symbol} sync token job created or update status {status}, message:{message}.",
            input.Symbol, input.Status, input.Message);

        return new GrainResultDto<SynchronizeAITokenJobGrainDto>()
        {
            Data = _objectMapper.Map<SynchronizeAITokenState, SynchronizeAITokenJobGrainDto>(State),
        };
    }

    public async Task<GrainResultDto<SynchronizeAITokenJobGrainDto>> GetSynchronizeAITokenJobAsync()
    {
        return new GrainResultDto<SynchronizeAITokenJobGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<SynchronizeAITokenState, SynchronizeAITokenJobGrainDto>(State)
        };
    }


    [ExceptionHandler(typeof(Exception),
        Message = "SynchronizeAITokenJobGrain An error occurred during job execution and will be retried",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new[] { "input" }
    )]
    public virtual async Task<GrainResultDto<SynchronizeAITokenJobGrainDto>> ExecuteJobAsync(SynchronizeAITokenJobGrainDto input)
    {
        State = _objectMapper.Map<SynchronizeAITokenJobGrainDto, SynchronizeAITokenState>(input);
        switch (State.Status)
        {
            case CrossCreateAITokenStatus.TokenCreating:
                await HandleTokenCreatingAsync();
                break;
            case CrossCreateAITokenStatus.TokenValidating:
                await HandleTokenValidatingAsync();
                break;
            case CrossCreateAITokenStatus.WaitingIndexing:
                await HandleWaitingIndexingAsync();
                break;
            case CrossCreateAITokenStatus.CrossChainTokenCreating:
                await HandleCrossChainTokenCreatingAsync();
                break;
        }

        return new GrainResultDto<SynchronizeAITokenJobGrainDto>()
        {
            Data = _objectMapper.Map<SynchronizeAITokenState, SynchronizeAITokenJobGrainDto>(State),
        };
    }
    private async Task<bool> ValidateTokenAsync(string symbol)
    {
        var tokenAddress = _chainOptionsMonitor.CurrentValue.ChainInfos[State.FromChainId].TokenContractAddress;
        var tokenInfo = await CallTransactionAsync<TokenInfo>(State.FromChainId,
            await GenerateRawTransaction(MethodName.GetTokenInfo, new GetTokenInfoInput
            {
                Symbol = symbol
            }, State.FromChainId, tokenAddress));
        if (tokenInfo == null || tokenInfo.Symbol != symbol)
        {
            return false;
        }

        State.Symbol = tokenInfo.Symbol;
        State.ValidateTokenTx = await GenerateRawTransaction(MethodName.ValidateTokenInfoExists,
            new ValidateTokenInfoExistsInput
            {
                Symbol = tokenInfo.Symbol,
                TokenName = tokenInfo.TokenName,
                Decimals = tokenInfo.Decimals,
                IsBurnable = tokenInfo.IsBurnable,
                IssueChainId = tokenInfo.IssueChainId,
                Issuer = new Address { Value = tokenInfo.Issuer.Value },
                TotalSupply = tokenInfo.TotalSupply,
                Owner = tokenInfo.Owner,
                ExternalInfo = { tokenInfo.ExternalInfo.Value }
            }, State.FromChainId, tokenAddress);
        var txRes = await SendTransactionAsync(State.FromChainId, State.ValidateTokenTx);
        State.Symbol = symbol;
        State.ValidateTokenTxId = txRes.TransactionId;

        return true;
    }
    private async Task HandleTokenCreatingAsync()
    {
        var symbol = State.Symbol;
        if (!await ValidateTokenAsync(symbol))
        {
            State.Status = SynchronizeTransactionJobStatus.Failed;
            await WriteStateAsync();
            return;
        }

        State.Status = SynchronizeTransactionJobStatus.TokenValidating;
        _logger.LogInformation("Symbol is {symbol} update status to {status} in HandleTokenCreatingAsync.",
            State.Symbol, State.Status);
    }

    private async Task HandleTokenValidatingAsync()
    {
        var txResult = await GetTxResultAsync(State.FromChainId, State.ValidateTokenTxId);
        if (!await CheckTxStatusAsync(txResult)) return;
        if (txResult.BlockNumber == 0) return;

        State.ValidateTokenHeight = txResult.BlockNumber;
        State.Status = SynchronizeTransactionJobStatus.WaitingIndexing;

        _logger.LogInformation("Symbol id {Symbol} update status to {status} in HandleTokenValidatingAsync.",
            State.Symbol, State.Status);

        await WriteStateAsync();
    }

    private async Task HandleWaitingIndexingAsync()
    {
        if (!await CrossChainCreateTokenAsync()) return;
        State.Status = SynchronizeTransactionJobStatus.CrossChainTokenCreating;
        _logger.LogInformation("Symbol is {Symbol} update status to {status} in HandleWaitingIndexingAsync.",
            State.Symbol, State.Status);
        await WriteStateAsync();
    }

    private async Task HandleCrossChainTokenCreatingAsync()
    {
        var txResult = await GetTxResultAsync(State.ToChainId, State.CrossChainCreateTokenTxId);
        if (!await CheckTxStatusAsync(txResult)) return;

        State.Status = SynchronizeTransactionJobStatus.CrossChainTokenCreated;

        _logger.LogInformation("Symbol is {Symbol} update status to {status} in HandleCrossChainTokenCreatingAsync.",
            State.Symbol, State.Status);

        await WriteStateAsync();
    }
    private async Task<TransactionResultDto> GetTxResultAsync(string chainId, string txId)
    {
        var client = _blockchainClientFactory.GetClient(chainId);
        return await client.GetTransactionResultAsync(txId);
    }
    [ExceptionHandler(typeof(Exception),
        Message = "SynchronizeTxJobGrain.GetMerklePathAsync There was an error getting the merkle path, try again later", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new []{"chainId", "txId"}
    )]
    public virtual async Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
    {
        var client = _blockchainClientFactory.GetClient(chainId);
        return await client.GetMerklePathByTransactionIdAsync(txId);
    }

    private async Task<long> GetIndexHeightAsync(string chainId)
    {
        var chainInfo = _chainOptionsMonitor.CurrentValue.ChainInfos[chainId];

        var client = _blockchainClientFactory.GetClient(chainId);

        var transaction = await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(chainInfo.PrivateKey),
            chainInfo.CrossChainContractAddress, MethodName.GetParentChainHeight, new Empty());
        var txWithSign = client.SignTransaction(chainInfo.PrivateKey, transaction);

        var transactionGetTokenResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        var result = Int64Value.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionGetTokenResult));

        return result.Value;
    }
    private async Task<bool> CheckTxStatusAsync(TransactionResultDto txResult)
    {
        if (txResult.Status == TransactionState.Mined) return true;

        if (txResult.Status == TransactionState.Pending) return false;

        // When Transaction status is not mined or pending, Transaction is judged to be failed.
        State.Message = $"Transaction failed, status: {State.Status}. error: {txResult.Error}";
        State.Status = CrossCreateAITokenStatus.Failed;

        await WriteStateAsync();
        _logger.LogWarning("Transaction failed, Symbol is {Symbol} update status to {status}.",
            State.Symbol, State.Status);

        return false;
    }
    private async Task<string> GenerateRawTransaction(string methodName, IMessage param, string chainId,
        string contractAddress)
    {
        if (!_chainOptionsMonitor.CurrentValue.ChainInfos.TryGetValue(chainId, out var chainInfo)) return "";

        var client = _blockchainClientFactory.GetClient(chainId);
        return client.SignTransaction(chainInfo.PrivateKey, await client.GenerateTransactionAsync(
                client.GetAddressFromPrivateKey(chainInfo.PrivateKey), contractAddress, methodName, param))
            .ToByteArray().ToHex();
    }
    private async Task<T> CallTransactionAsync<T>(string chainId, string rawTx) where T : class, IMessage<T>, new()
    {
        var client = _blockchainClientFactory.GetClient(chainId);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto() { RawTransaction = rawTx });
        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));
        return value;
    }
    private async Task<SendTransactionOutput> SendTransactionAsync(string chainId, string rawTx)
    {
        var client = _blockchainClientFactory.GetClient(chainId);
        return await client.SendTransactionAsync(new SendTransactionInput() { RawTransaction = rawTx });
    }
    private async Task<bool> CrossChainCreateTokenAsync()
    {
        var indexHeight = await GetIndexHeightAsync(State.ToChainId);
        if (indexHeight < State.ValidateTokenHeight)
        {
            _logger.LogInformation("[Synchronize Job]Now index height {indexHeight}, expected height:{ValidateHeight}",
                indexHeight, State.ValidateTokenHeight);
            return false;
        }

        var chainOptions = _chainOptionsMonitor.CurrentValue;
        var merklePath = await GetMerklePathAsync(State.FromChainId, State.ValidateTokenTxId);
        if (merklePath == null) return false;

        var createTokenParams = new CrossChainCreateTokenInput
        {
            FromChainId = ChainHelper.ConvertBase58ToChainId(chainOptions.ChainInfos[State.FromChainId].ChainId),
            ParentChainHeight = State.ValidateTokenHeight,
            TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(State.ValidateTokenTx)),
            MerklePath = new MerklePath()
        };

        foreach (var node in merklePath.MerklePathNodes)
        {
            createTokenParams.MerklePath.MerklePathNodes.Add(new MerklePathNode()
            {
                Hash = new Hash() { Value = Hash.LoadFromHex(node.Hash).Value },
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        var txId = await SendTransactionAsync(State.ToChainId,
            await GenerateRawTransaction(MethodName.CrossChainCreateToken, createTokenParams,
                State.ToChainId, chainOptions.ChainInfos[State.ToChainId].TokenContractAddress));

        State.CrossChainCreateTokenTxId = txId.TransactionId;
        _logger.LogInformation("CrossChainCreateTokenTxId {TxId}", txId.TransactionId);
        return true;
    }

}