using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using AElf.Contracts.TokenAdapterContract;
using AElf.Types;
using Forest.Contracts.Auction;
using Forest.SymbolRegistrar;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.State.Synchronize;
using Orleans;
using Volo.Abp.ObjectMapping;
using AuctionConfig = Forest.Contracts.Auction.AuctionConfig;
using AuctionType = Forest.Contracts.Auction.AuctionType;
using Price = Forest.Contracts.Auction.Price;

// using TokenInfo = AElf.Client.MultiToken.TokenInfo;

namespace NFTMarketServer.Grains.Grain.Synchronize;

public class SynchronizeTxJobGrain : Grain<SynchronizeState>, ISynchronizeTxJobGrain
{
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly IOptionsMonitor<SynchronizeSeedJobOptions> _seedOptionsMonitor;
    private readonly ILogger<SynchronizeTxJobGrain> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;
    private const string DefaultFromChain = "AELF";


    public SynchronizeTxJobGrain(ILogger<SynchronizeTxJobGrain> logger,
        IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        IObjectMapper objectMapper, IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsMonitor<SynchronizeSeedJobOptions> seedOptionsMonitor)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _blockchainClientFactory = blockchainClientFactory;
        _seedOptionsMonitor = seedOptionsMonitor;
        _chainOptionsMonitor = chainOptionsMonitor;
    }

    public async Task<GrainResultDto<SynchronizeTxJobGrainDto>> CreateSynchronizeTransactionJobAsync(
        CreateSynchronizeTransactionJobGrainDto input)
    {
        if (State.Id == string.Empty)
        {
            State.Id = input.TxHash;
        }

        State = _objectMapper.Map<CreateSynchronizeTransactionJobGrainDto, SynchronizeState>(input);
        await WriteStateAsync();
        _logger.LogInformation("TxHash id {txHash} sync job created in CreateSynchronizeTransactionJobAsync.",
            State.TxHash);

        return new GrainResultDto<SynchronizeTxJobGrainDto>()
        {
            Data = _objectMapper.Map<SynchronizeState, SynchronizeTxJobGrainDto>(State),
        };
    }

    public async Task<GrainResultDto<SynchronizeTxJobGrainDto>> CreateSeedJobAsync(CreateSeedJobGrainDto input)
    {
        if (State.Id == string.Empty)
        {
            State.Id = input.Id;
        }

        State = _objectMapper.Map<CreateSeedJobGrainDto, SynchronizeState>(input);

        // Now only supports crossing from AELF to tDVV or tDVW
        State.FromChainId = DefaultFromChain;
        State.ToChainId = _seedOptionsMonitor.CurrentValue.ToChainId;
        var chainOptions = _chainOptionsMonitor.CurrentValue;
        var specialSeed = await CallTransactionAsync<SpecialSeed>(State.FromChainId, await GenerateRawTransaction(
            MethodName.GetSpecialSeed, new StringValue { Value = State.Seed }, State.FromChainId,
            chainOptions.ChainInfos[State.FromChainId].SymbolRegistrarContractAddress));
        if (specialSeed.SeedType != SeedType.Unique)
        {
            State.Status = SynchronizeTransactionJobStatus.Failed;
            State.Message = $"Seed {State.Symbol} SeedType is not Unique.";
            await WriteStateAsync();
        }

        State.Seed = specialSeed.Symbol;

        var txRes = await SendTransactionAsync(State.FromChainId, await GenerateRawTransaction(
            MethodName.CreateSeed, new CreateSeedInput
            {
                Symbol = State.Seed,
                To = Address.FromBase58(_blockchainClientFactory.GetClient(State.FromChainId)
                    .GetAddressFromPrivateKey(chainOptions.ChainInfos[State.FromChainId].PrivateKey)),
            }, State.FromChainId, chainOptions.ChainInfos[State.FromChainId].SymbolRegistrarContractAddress));
        State.TxHash = txRes.TransactionId;
        State.Status = SynchronizeTransactionJobStatus.SeedCreated;

        await WriteStateAsync();
        _logger.LogInformation("Seed {seed} job created in CreateSeedJobAsync.", State.Symbol);

        return new GrainResultDto<SynchronizeTxJobGrainDto>()
        {
            Data = _objectMapper.Map<SynchronizeState, SynchronizeTxJobGrainDto>(State),
        };
    }

    public async Task<GrainResultDto<SynchronizeTxJobGrainDto>> ExecuteJobAsync(
        SynchronizeTxJobGrainDto input)
    {
        State = _objectMapper.Map<SynchronizeTxJobGrainDto, SynchronizeState>(input);

        try
        {
            switch (State.Status)
            {
                // ProxyAccountsAndToken
                case SynchronizeTransactionJobStatus.ProxyAccountsAndTokenCreating:
                    await HandleProxyAccountAndTokenCreatingAsync();
                    break;
                case SynchronizeTransactionJobStatus.ProxyAccountsAndTokenValidating:
                    await HandleProxyAccountAndTokenValidatingAsync();
                    break;
                case SynchronizeTransactionJobStatus.WaitingProxyAccountsAndTokenIndexing:
                    await HandleWaitingProxyAccountAndTokenIndexingAsync();
                    break;
                case SynchronizeTransactionJobStatus.CrossChainProxyAccountsAndTokenSyncing:
                    await HandleCrossChainProxyAccountAndTokenSyncingAsync();
                    break;
                // Collection|NFT
                case SynchronizeTransactionJobStatus.TokenCreating:
                    await HandleTokenCreatingAsync();
                    break;
                case SynchronizeTransactionJobStatus.TokenValidating:
                    await HandleTokenValidatingAsync();
                    break;
                case SynchronizeTransactionJobStatus.WaitingIndexing:
                    await HandleWaitingIndexingAsync();
                    break;
                case SynchronizeTransactionJobStatus.CrossChainTokenCreating:
                    await HandleCrossChainTokenCreatingAsync();
                    break;
                // Seed
                case SynchronizeTransactionJobStatus.SeedCreated:
                    await HandleSeedCreatedAsync();
                    break;
                case SynchronizeTransactionJobStatus.SeedValidating:
                    await HandleSeedValidatingAsync();
                    break;
                case SynchronizeTransactionJobStatus.SeedWaitingIndexing:
                    await HandleSeedWaitingIndexingAsync();
                    break;
                case SynchronizeTransactionJobStatus.SeedCrossChainReceiving:
                    await HandleSeedCrossChainReceivingAsync();
                    break;
                case SynchronizeTransactionJobStatus.SeedCrossChainCreating:
                    await HandleSeedCrossChainCreatingAsync();
                    break;
                case SynchronizeTransactionJobStatus.SeedCreateAuction:
                    await HandleSeedCreateAuctionAsync();
                    break;
                case SynchronizeTransactionJobStatus.AuctionCreating:
                    await HandleSeedCreateAuctioningAsync();
                    break;
            }

            return new GrainResultDto<SynchronizeTxJobGrainDto>()
            {
                Data = _objectMapper.Map<SynchronizeState, SynchronizeTxJobGrainDto>(State),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during job execution and will be retried: {TxHash}", State.TxHash);
            return new GrainResultDto<SynchronizeTxJobGrainDto>()
            {
                Data = _objectMapper.Map<SynchronizeState, SynchronizeTxJobGrainDto>(State),
                Success = false
            };
        }
    }

    #region Seed Create and crossChain

    private async Task HandleSeedCreatedAsync()
    {
        if (!await ValidateTokenAsync()) return;
        // transfer seed to side chain
        var _chainOptions = _chainOptionsMonitor.CurrentValue;
        State.CrossChainTransferTx = await GenerateRawTransaction(MethodName.CrossChainTransfer,
            new CrossChainTransferInput
            {
                To = Address.FromBase58(_blockchainClientFactory.GetClient(State.ToChainId)
                    .GetAddressFromPrivateKey(_chainOptions.ChainInfos[State.ToChainId].PrivateKey)),
                Symbol = State.Symbol,
                Amount = 1,
                Memo = "",
                ToChainId = ChainHelper.ConvertBase58ToChainId(State.ToChainId),
                IssueChainId = ChainHelper.ConvertBase58ToChainId(State.FromChainId)
            }, State.FromChainId, _chainOptions.ChainInfos[State.FromChainId].TokenContractAddress);
        var txId = await SendTransactionAsync(State.FromChainId, State.CrossChainTransferTx);
        State.CrossChainTransferTxId = txId.TransactionId;

        State.Status = SynchronizeTransactionJobStatus.SeedValidating;
        _logger.LogInformation("Seed {seed} update status to {status} in HandleSeedCreatedAsync.", State.Symbol,
            State.Status);
        await WriteStateAsync();
    }

    private async Task HandleSeedValidatingAsync()
    {
        var txResult = await GetTxResultAsync(State.FromChainId, State.ValidateTokenTxId);
        if (!await CheckTxStatusAsync(txResult)) return;
        if (txResult.BlockNumber == 0) return;

        var transferTxResult = await GetTxResultAsync(State.FromChainId, State.CrossChainTransferTxId);
        if (!await CheckTxStatusAsync(transferTxResult)) return;
        if (transferTxResult.BlockNumber == 0) return;

        State.ValidateTokenHeight = txResult.BlockNumber;
        State.CrossChainTransferHeight = transferTxResult.BlockNumber;

        State.Status = SynchronizeTransactionJobStatus.SeedWaitingIndexing;
        _logger.LogInformation("Seed {seed} update status to {status} in HandleSeedValidatingAsync.", State.Symbol,
            State.Status);
        await WriteStateAsync();
    }

    private async Task HandleSeedWaitingIndexingAsync()
    {
        if (!await CrossChainCreateTokenAsync()) return;
        State.Status = SynchronizeTransactionJobStatus.SeedCrossChainReceiving;
        _logger.LogInformation("TxHash id {txHash} update status to {status} in HandleWaitingIndexingAsync.",
            State.TxHash, State.Status);
        await WriteStateAsync();
    }

    private async Task HandleSeedCrossChainReceivingAsync()
    {
        var txResult = await GetTxResultAsync(State.ToChainId, State.CrossChainCreateTokenTxId);
        if (!await CheckTxStatusAsync(txResult)) return;
        
        var indexHeight = await GetIndexHeightAsync(State.ToChainId);
        if (indexHeight < State.CrossChainTransferHeight)
        {
            _logger.LogInformation("[Synchronize Job]Now index height {indexHeight}, expected height:{ValidateHeight}",
                indexHeight, State.CrossChainTransferHeight);
            return;
        }

        var merklePathDto = await GetMerklePathAsync(State.FromChainId, State.CrossChainTransferTxId);
        if (merklePathDto == null) return;
        var merklePath = new MerklePath();
        foreach (var node in merklePathDto.MerklePathNodes)
        {
            merklePath.MerklePathNodes.Add(new MerklePathNode
            {
                Hash = new Hash { Value = Hash.LoadFromHex(node.Hash).Value },
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        var chainOptions = _chainOptionsMonitor.CurrentValue;
        var txRes = await SendTransactionAsync(State.ToChainId, await GenerateRawTransaction(
            MethodName.CrossChainReceiveToken, new CrossChainReceiveTokenInput
            {
                FromChainId = ChainHelper.ConvertBase58ToChainId(chainOptions.ChainInfos[State.FromChainId].ChainId),
                ParentChainHeight = State.CrossChainTransferHeight,
                TransferTransactionBytes =
                    ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(State.CrossChainTransferTx)),
                MerklePath = merklePath
            }, State.ToChainId, chainOptions.ChainInfos[State.ToChainId].TokenContractAddress));
        State.SeedCrossChainReceivedTxId = txRes.TransactionId;
        State.Status = SynchronizeTransactionJobStatus.SeedCrossChainCreating;
        _logger.LogInformation("TxHash id {txHash} update status to {status} in HandleWaitingIndexingAsync.",
            State.TxHash, State.Status);
        await WriteStateAsync();
    }


    private async Task HandleSeedCrossChainCreatingAsync()
    {
        var transferTxResult = await GetTxResultAsync(State.ToChainId, State.SeedCrossChainReceivedTxId);
        if (!await CheckTxStatusAsync(transferTxResult)) return;

        var txRes = await SendTransactionAsync(State.ToChainId, await GenerateRawTransaction(MethodName.Approve,
            new ApproveInput
            {
                Symbol = State.Symbol,
                Amount = 1,
                Spender = Address.FromBase58(_chainOptionsMonitor.CurrentValue.ChainInfos[State.ToChainId]
                    .AuctionContractAddress)
            }, State.ToChainId, _chainOptionsMonitor.CurrentValue.ChainInfos[State.ToChainId].TokenContractAddress));
        State.SeedApprovedTxId = txRes.TransactionId;
        State.Status = SynchronizeTransactionJobStatus.SeedCreateAuction;
        _logger.LogInformation("Seed {seed} update status to {status} in HandleSeedCrossChainCreatingAsync.",
            State.Symbol,
            State.Status);
        await WriteStateAsync();
    }

    private async Task HandleSeedCreateAuctionAsync()
    {
        var txResult = await GetTxResultAsync(State.ToChainId, State.SeedApprovedTxId);
        if (!await CheckTxStatusAsync(txResult)) return;

        var chainOptions = _chainOptionsMonitor.CurrentValue;
        var specialSeed = await CallTransactionAsync<SpecialSeed>(State.FromChainId, await GenerateRawTransaction(
            MethodName.GetSpecialSeed, new StringValue { Value = State.Seed }, State.FromChainId,
            chainOptions.ChainInfos[State.FromChainId].SymbolRegistrarContractAddress));
        var txRes = await SendTransactionAsync(State.ToChainId, await GenerateRawTransaction(MethodName.CreateAuction,
            new CreateAuctionInput
            {
                AuctionType = AuctionType.English,
                Symbol = State.Symbol,
                AuctionConfig = await CallTransactionAsync<AuctionConfig>(State.FromChainId,
                    await GenerateRawTransaction(MethodName.GetAuctionConfig, new Empty(), State.FromChainId,
                        chainOptions.ChainInfos[State.FromChainId].SymbolRegistrarContractAddress)),
                StartPrice = new Price
                {
                    Symbol = specialSeed.PriceSymbol,
                    Amount = specialSeed.PriceAmount
                },
                ReceivingAddress = Address.FromBase58(chainOptions.ChainInfos[State.ToChainId].ReceivingAddress)
            }, State.ToChainId, chainOptions.ChainInfos[State.ToChainId].AuctionContractAddress));
        State.CreateAuctionTxId = txRes.TransactionId;
        State.Status = SynchronizeTransactionJobStatus.AuctionCreating;
        _logger.LogInformation("Seed {seed} update status to {status} in HandleSeedCreateAuctionAsync.", State.Symbol,
            State.Status);
        await WriteStateAsync();
    }

    private async Task HandleSeedCreateAuctioningAsync()
    {
        var txResult = await GetTxResultAsync(State.ToChainId, State.CreateAuctionTxId);
        if (!await CheckTxStatusAsync(txResult)) return;
        State.Status = SynchronizeTransactionJobStatus.AuctionCreated;
        _logger.LogInformation("Seed {seed} update status to {status} in HandleSeedCreateAuctioningAsync.",
            State.Symbol,
            State.Status);
        await WriteStateAsync();
    }

    #endregion

    # region Token crossChain

    private async Task HandleTokenCreatingAsync()
    {
        if (!await ValidateTokenAsync()) return;
        State.Status = SynchronizeTransactionJobStatus.TokenValidating;
        _logger.LogInformation("TxHash id {txHash} update status to {status} in HandleTokenCreatingAsync.",
            State.TxHash, State.Status);
        await WriteStateAsync();
    }

    private async Task HandleTokenValidatingAsync()
    {
        var txResult = await GetTxResultAsync(State.FromChainId, State.ValidateTokenTxId);
        if (!await CheckTxStatusAsync(txResult)) return;
        if (txResult.BlockNumber == 0) return;

        State.ValidateTokenHeight = txResult.BlockNumber;
        State.Status = SynchronizeTransactionJobStatus.WaitingIndexing;

        _logger.LogInformation("TxHash id {txHash} update status to {status} in HandleTokenValidatingAsync.",
            State.TxHash, State.Status);

        await WriteStateAsync();
    }

    private async Task HandleWaitingIndexingAsync()
    {
        if (!await CrossChainCreateTokenAsync()) return;
        State.Status = SynchronizeTransactionJobStatus.CrossChainTokenCreating;
        _logger.LogInformation("TxHash id {txHash} update status to {status} in HandleWaitingIndexingAsync.",
            State.TxHash, State.Status);
        await WriteStateAsync();
    }

    private async Task HandleCrossChainTokenCreatingAsync()
    {
        var txResult = await GetTxResultAsync(State.ToChainId, State.CrossChainCreateTokenTxId);
        if (!await CheckTxStatusAsync(txResult)) return;

        State.Status = SynchronizeTransactionJobStatus.CrossChainTokenCreated;

        _logger.LogInformation("TxHash id {txHash} update status to {status} in HandleCrossChainTokenCreatingAsync.",
            State.TxHash, State.Status);

        await WriteStateAsync();
    }

    #endregion

    #region ProxyAccount crossChain

    private async Task HandleProxyAccountAndTokenCreatingAsync()
    {
        var txResult = await GetTxResultAsync(State.FromChainId, State.TxHash);
        if (!await CheckTxStatusAsync(txResult)) return;
        var accountAddress = _chainOptionsMonitor.CurrentValue.ChainInfos[State.FromChainId].ProxyAccountAddress;
        var adapterAddress =
            _chainOptionsMonitor.CurrentValue.ChainInfos[State.FromChainId].TokenAdapterContractAddress;
        _logger.LogInformation(
            "HandleProxyAccountAndTokenCreatingAsync {FromChainId},{TxHash},{txResult},{accountAddress},{adapterAddress}",
            State.FromChainId,
            State.TxHash, JsonConvert.SerializeObject(txResult), accountAddress, adapterAddress);
        var eventRes = await GetLogEvents<ManagerTokenCreated>(txResult, nameof(ManagerTokenCreated), adapterAddress);

        if (eventRes == null) return;

        var ownerInfo = await CallTransactionAsync<ProxyAccount>(State.FromChainId,
            await GenerateRawTransaction(MethodName.GetProxyAccountByHash, eventRes.OwnerVirtualHash,
                State.FromChainId, accountAddress));
        var issuerInfo = await CallTransactionAsync<ProxyAccount>(State.FromChainId,
            await GenerateRawTransaction(MethodName.GetProxyAccountByHash, eventRes.IssuerVirtualHash,
                State.FromChainId, accountAddress));
        if (ownerInfo.ProxyAccountHash != eventRes.OwnerVirtualHash ||
            issuerInfo.ProxyAccountHash != eventRes.IssuerVirtualHash)
        {
            _logger.LogError("Get proxyAccount fail, chain: {chainId}, owner: {proxyAccount}, issuer: {proxyAccount}.",
                State.FromChainId, eventRes.OwnerVirtualHash.ToHex(), eventRes.IssuerVirtualHash.ToHex());
            State.Status = SynchronizeTransactionJobStatus.Failed;
            State.Message = $"Validate ProxyAccount exists failed, status: {State.Status}.";
            await WriteStateAsync();
            return;
        }

        State.ValidateOwnerAgentTx = await GenerateRawTransaction(
            MethodName.ValidateProxyAccountExists, new ValidateProxyAccountExistsInput
            {
                ManagementAddresses = { ownerInfo.ManagementAddresses },
                CreateChainId = ownerInfo.CreateChainId,
                ProxyAccountHash = ownerInfo.ProxyAccountHash
            }, State.FromChainId, accountAddress);
        var ownerTxRes = await SendTransactionAsync(State.FromChainId, State.ValidateOwnerAgentTx);
        State.ValidateOwnerAgentTxId = ownerTxRes.TransactionId;

        State.ValidateIssuerAgentTx = await GenerateRawTransaction(
            MethodName.ValidateProxyAccountExists, new ValidateProxyAccountExistsInput
            {
                ManagementAddresses = { issuerInfo.ManagementAddresses },
                CreateChainId = issuerInfo.CreateChainId,
                ProxyAccountHash = issuerInfo.ProxyAccountHash
            }, State.FromChainId, accountAddress);
        var issuerTxRes = await SendTransactionAsync(State.FromChainId, State.ValidateIssuerAgentTx);
        State.ValidateIssuerAgentTxId = issuerTxRes.TransactionId;

        // handle token creating
        await HandleTokenCreatingAsync();

        // In order to avoid errors when validating token creating.
        State.Status = State.Status == SynchronizeTransactionJobStatus.Failed
            ? SynchronizeTransactionJobStatus.Failed
            : SynchronizeTransactionJobStatus.ProxyAccountsAndTokenValidating;

        _logger.LogInformation(
            "TxHash id {txHash} update status to {status} in HandleProxyAccountAndTokenCreatingAsync.",
            State.TxHash, State.Status);
        await WriteStateAsync();
    }

    private async Task HandleProxyAccountAndTokenValidatingAsync()
    {
        // check ProxyAccount and Token validate tx result
        var txResult = await GetTxResultAsync(State.FromChainId, State.ValidateTokenTxId);
        if (!await CheckTxStatusAsync(txResult) || txResult.BlockNumber == 0) return;
        var validateIssuerAgentTxResult =
            await GetTxResultAsync(State.FromChainId, State.ValidateIssuerAgentTxId);
        if (!await CheckTxStatusAsync(validateIssuerAgentTxResult) ||
            validateIssuerAgentTxResult.BlockNumber == 0) return;
        var validateOwnerAgentTxResult =
            await GetTxResultAsync(State.FromChainId, State.ValidateOwnerAgentTxId);
        if (!await CheckTxStatusAsync(validateOwnerAgentTxResult) ||
            validateOwnerAgentTxResult.BlockNumber == 0) return;

        State.ValidateTokenHeight = txResult.BlockNumber;
        State.ValidateIssuerAgentHeight = validateIssuerAgentTxResult.BlockNumber;
        State.ValidateOwnerAgentHeight = validateOwnerAgentTxResult.BlockNumber;
        State.Status = SynchronizeTransactionJobStatus.WaitingProxyAccountsAndTokenIndexing;

        _logger.LogInformation(
            "TxHash id {txHash} update status to {status} in HandleProxyAccountAndTokenValidatingAsync.",
            State.TxHash, State.Status);
        await WriteStateAsync();
    }

    private async Task HandleWaitingProxyAccountAndTokenIndexingAsync()
    {
        var indexHeight = await GetIndexHeightAsync(State.ToChainId);
        if (indexHeight < State.ValidateTokenHeight)
        {
            _logger.LogInformation("[Indexing]Now: {indexHeight}, expected:{ValidateHeight}", indexHeight,
                State.ValidateTokenHeight);
            return;
        }

        await HandleWaitingIndexingAsync();

        var issuerAgentTxId = await CrossChainSyncProxyAccountAsync(State.ValidateIssuerAgentHeight,
            State.ValidateIssuerAgentTxId, State.ValidateIssuerAgentTx);
        if (string.IsNullOrEmpty(issuerAgentTxId)) return;
        var ownerAgentTxId = await CrossChainSyncProxyAccountAsync(State.ValidateOwnerAgentHeight,
            State.ValidateOwnerAgentTxId, State.ValidateOwnerAgentTx);
        if (string.IsNullOrEmpty(ownerAgentTxId)) return;

        State.CrossChainSyncIssuerAgentTxId = issuerAgentTxId;
        State.CrossChainSyncOwnerAgentTxId = ownerAgentTxId;
        State.Status = SynchronizeTransactionJobStatus.CrossChainProxyAccountsAndTokenSyncing;
        _logger.LogInformation(
            "TxHash id {txHash} update status to {status} in HandleWaitingProxyAccountAndTokenIndexingAsync.",
            State.TxHash, State.Status);
        await WriteStateAsync();
    }

    private async Task<string> CrossChainSyncProxyAccountAsync(long validateAgentHeight, string validateAgentTxId,
        string validateAgentTx)
    {
        var merklePath = await GetMerklePathAsync(State.FromChainId, validateAgentTxId);
        if (merklePath == null) return "";
        var chainOptions = _chainOptionsMonitor.CurrentValue;
        var crossChainParams = new CrossChainSyncProxyAccountInput()
        {
            FromChainId = ChainHelper.ConvertBase58ToChainId(chainOptions.ChainInfos[State.FromChainId].ChainId),
            ParentChainHeight = validateAgentHeight,
            TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(validateAgentTx)),
            MerklePath = new MerklePath()
        };

        foreach (var node in merklePath.MerklePathNodes)
        {
            crossChainParams.MerklePath.MerklePathNodes.Add(new MerklePathNode()
            {
                Hash = new Hash() { Value = Hash.LoadFromHex(node.Hash).Value },
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        var txId = await SendTransactionAsync(State.ToChainId,
            await GenerateRawTransaction(MethodName.CrossChainSyncProxyAccount, crossChainParams,
                State.ToChainId, chainOptions.ChainInfos[State.ToChainId].ProxyAccountAddress));
        var client = _blockchainClientFactory.GetClient(State.ToChainId);
        if (!_chainOptionsMonitor.CurrentValue.ChainInfos.TryGetValue(State.ToChainId, out var chainInfo)) return "";
        _logger.LogInformation("CrossChainSyncProxyAccountAsync oldTx:{oldTx} newTx:{txId} privateKey:{prikey} chainId:{chain} address:{add}", validateAgentTxId, txId, chainInfo.PrivateKey, State.ToChainId,client.GetAddressFromPrivateKey(chainInfo.PrivateKey));

        return txId.TransactionId;
    }

    private async Task HandleCrossChainProxyAccountAndTokenSyncingAsync()
    {
        var issuerAgentTxResult =
            await GetTxResultAsync(State.ToChainId, State.CrossChainSyncIssuerAgentTxId);
        var ownerAgentTxResult =
            await GetTxResultAsync(State.ToChainId, State.CrossChainSyncOwnerAgentTxId);
        var txResult =
            await GetTxResultAsync(State.ToChainId, State.CrossChainCreateTokenTxId);
        if (NeedCrossChainAgain(issuerAgentTxResult) || NeedCrossChainAgain(ownerAgentTxResult) ||
            NeedCrossChainAgain(txResult))
        {
            _logger.LogInformation("{txHash} cross chain create failed and will be re-created", State.TxHash);
            State.Status = SynchronizeTransactionJobStatus.WaitingProxyAccountsAndTokenIndexing;
            await WriteStateAsync();
            return;
        }

        // todo Handle agent proxy cross-chain failure
        if (!await CheckTxStatusAsync(issuerAgentTxResult) || !await CheckTxStatusAsync(ownerAgentTxResult)) return;

        // If the transaction is successful or the token exists, it is determined that the cross-chain is successful.
        if (await CheckTxStatusAsync(txResult) ||
            (!string.IsNullOrEmpty(txResult.Error) && txResult.Error.Contains("Token already exists")))
        {
            State.Status = SynchronizeTransactionJobStatus.CrossChainTokenCreated;
            _logger.LogInformation(
                "TxHash id {txHash} update status to {status} in HandleCrossChainProxyAccountAndTokenSyncingAsync.",
                State.TxHash, State.Status);
            await WriteStateAsync();
        }
    }

    #endregion

    #region common methods

    private async Task<bool> ValidateTokenAsync()
    {
        var txResult = await GetTxResultAsync(State.FromChainId, State.TxHash);
        if (!await CheckTxStatusAsync(txResult)) return false;

        var tokenAddress = _chainOptionsMonitor.CurrentValue.ChainInfos[State.FromChainId].TokenContractAddress;
        var eventRes = await GetLogEvents<TokenCreated>(txResult, nameof(TokenCreated), tokenAddress);
        if (eventRes == null) return false;

        var tokenInfo = await CallTransactionAsync<TokenInfo>(State.FromChainId,
            await GenerateRawTransaction(MethodName.GetTokenInfo, new GetTokenInfoInput
            {
                Symbol = eventRes.Symbol
            }, State.FromChainId, tokenAddress));
        if (tokenInfo.Symbol != eventRes.Symbol)
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
        State.Symbol = eventRes.Symbol;
        State.ValidateTokenTxId = txRes.TransactionId;

        return true;
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

    #endregion

    #region contract methods

    private async Task<bool> CheckTxStatusAsync(TransactionResultDto txResult)
    {
        if (txResult.Status == TransactionState.Mined) return true;

        if (txResult.Status == TransactionState.Pending) return false;

        // When Transaction status is not mined or pending, Transaction is judged to be failed.
        State.Message = $"Transaction failed, status: {State.Status}. error: {txResult.Error}";
        State.Status = SynchronizeTransactionJobStatus.Failed;

        await WriteStateAsync();
        _logger.LogInformation("Transaction failed, TxHash id {txHash} update status to {status}.",
            State.TxHash, State.Status);

        return false;
    }

    private async Task<T> GetLogEvents<T>(TransactionResultDto txResult, string logEventName, string contractAddress)
        where T : class, IMessage<T>, new()
    {
        var log = txResult.Logs.FirstOrDefault(l => l.Name == logEventName && l.Address == contractAddress);
        return TransactionLogEvent<T>(log);
    }

    private async Task<T> GetLogEvents<T>(TransactionResultDto txResult, string logEventName)
        where T : class, IMessage<T>, new()
    {
        var log = txResult.Logs.FirstOrDefault(l => l.Name == logEventName);
        return TransactionLogEvent<T>(log);
    }

    private static T TransactionLogEvent<T>(LogEventDto log) where T : class, IMessage<T>, new()
    {
        var logEvent = new LogEvent
        {
            Indexed = { log.Indexed.Select(ByteString.FromBase64) },
            NonIndexed = ByteString.FromBase64(log.NonIndexed)
        };

        var transactionLogEvent = new T();
        transactionLogEvent.MergeFrom(logEvent.NonIndexed);
        foreach (var indexed in logEvent.Indexed)
        {
            transactionLogEvent.MergeFrom(indexed);
        }

        return transactionLogEvent;
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

    private async Task<string> GenerateRawTransaction(string methodName, IMessage param, string chainId,
        string contractAddress)
    {
        if (!_chainOptionsMonitor.CurrentValue.ChainInfos.TryGetValue(chainId, out var chainInfo)) return "";

        var client = _blockchainClientFactory.GetClient(chainId);
        return client.SignTransaction(chainInfo.PrivateKey, await client.GenerateTransactionAsync(
                client.GetAddressFromPrivateKey(chainInfo.PrivateKey), contractAddress, methodName, param))
            .ToByteArray().ToHex();
        
    }

    private async Task<TransactionResultDto> GetTxResultAsync(string chainId, string txId)
    {
        var client = _blockchainClientFactory.GetClient(chainId);
        return await client.GetTransactionResultAsync(txId);
    }

    private async Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
    {
        try
        {
            var client = _blockchainClientFactory.GetClient(chainId);
            return await client.GetMerklePathByTransactionIdAsync(txId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{chainId}-{txId} There was an error getting the merkle path, try again later", chainId,
                txId);
            return null;
        }
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

    private bool NeedCrossChainAgain(TransactionResultDto txResult)
    {
        return !string.IsNullOrEmpty(txResult.Error) && txResult.Error.Contains("Parent chain block at height");
    }

    #endregion
}