using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Basic;
using NFTMarketServer.Bid;
using NFTMarketServer.Common;
using NFTMarketServer.Grains.Grain.Synchronize;
using NFTMarketServer.HandleException;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Synchronize.Dto;
using NFTMarketServer.Synchronize.Eto;
using NFTMarketServer.Synchronize.Provider;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace NFTMarketServer.Synchronize;

public class SynchronizeAppService : NFTMarketServerAppService, ISynchronizeAppService
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<SynchronizeAppService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ISynchronizeTransactionProvider _synchronizeTransactionProvider;
    private readonly IBidAppService _bidAppService;

    public SynchronizeAppService(ISynchronizeTransactionProvider synchronizeTransactionProvider,
        IClusterClient clusterClient, IObjectMapper objectMapper, ILogger<SynchronizeAppService> logger,
        IBidAppService bidAppService,
        IDistributedEventBus distributedEventBus)
    {
        _synchronizeTransactionProvider = synchronizeTransactionProvider;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _logger = logger;
        _distributedEventBus = distributedEventBus;
        _bidAppService = bidAppService;
    }

    public async Task<SyncResultDto> GetSyncResultByTxHashAsync(GetSyncResultByTxHashDto input)
    {
        // var userId = input.UserId;
        var userId = CurrentUser.GetId();
        _logger.LogDebug("GetSyncResultByTxHashAsync userId={userId} TxHash={TxHash}", userId, input.TxHash);

        var syncTxData =
            await _synchronizeTransactionProvider.GetSynchronizeJobByTxHashAsync(userId, input.TxHash);
        if (syncTxData == null || syncTxData.TxHash != input.TxHash)
        {
            _logger.LogError("GetSyncResultByTxHashAsync No transaction hash {TxHash} found.", input.TxHash);
            return new SyncResultDto();
        }

        return _objectMapper.Map<SynchronizeTransactionDto, SyncResultDto>(syncTxData);
    }

    public async Task<SyncResultDto> GetSyncResultForAuctionSeedByTxHashAsync(GetSyncResultByTxHashDto input)
    {
        _logger.LogDebug("GetSyncResultForAuctionSeedByTxHashAsync TxHash={TxHash}", input.TxHash);

        var syncTxData =
            await _synchronizeTransactionProvider.GetSynchronizeJobByTxHashAsync(null, input.TxHash);
        if (syncTxData == null || syncTxData.TxHash != input.TxHash || syncTxData.Symbol.IsNullOrEmpty())
        {
            _logger.LogError("GetSyncResultForAuctionSeedByTxHashAsync No transaction hash {TxHash} found.",
                input.TxHash);
            return new SyncResultDto();
        }

        var auctionInfo = await _bidAppService.GetSymbolAuctionInfoAsync(syncTxData.Symbol);
        if (auctionInfo == null)
        {
            _logger.LogError(
                "GetSyncResultForAuctionSeedByTxHashAsync transaction hash {TxHash} No AuctionInfo found.{symbol}",
                input.TxHash, syncTxData.Symbol);
            return new SyncResultDto();
        }

        return _objectMapper.Map<SynchronizeTransactionDto, SyncResultDto>(syncTxData);
    }

    [ExceptionHandler(typeof(Exception),
        Message = "SynchronizeAppService.SendNFTSyncAsync An error occurred during the creation of the synchronous job",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new[] { "input" }
    )]
    public virtual async Task<SendNFTSyncResponseDto> SendNFTSyncAsync(SendNFTSyncDto input)
    {
        // var userId = input.UserId;
        var userId = CurrentUser.GetId();

        var syncTxData =
            await _synchronizeTransactionProvider.GetSynchronizeJobByTxHashAsync(userId, input.TxHash);

        if (syncTxData.TxHash != null && string.IsNullOrEmpty(syncTxData.Status))
        {
            _logger.LogError("This transaction {TxHash} status {Status} had registry.", input.TxHash,
                syncTxData.Status);
            throw new UserFriendlyException(
                $"This transaction {input.TxHash} status {syncTxData.Status} had registry.");
        }

        var synchronizeTransactionJobGrain = _clusterClient.GetGrain<ISynchronizeTxJobGrain>(input.TxHash);

        var createSyncTransJobDto =
            _objectMapper.Map<SendNFTSyncDto, CreateSynchronizeTransactionJobGrainDto>(input);
        createSyncTransJobDto.Status = DetermineTheSynchronizationJobInitStatus(input.Symbol);

        _logger.LogInformation("TxHash: {TxHash} Synchronize-Transaction-Job will be created",
            createSyncTransJobDto.TxHash);
        var result =
            await synchronizeTransactionJobGrain.CreateSynchronizeTransactionJobAsync(createSyncTransJobDto);

        if (!result.Success)
        {
            _logger.LogError(
                "Create Synchronize Transaction Job fail, user id: {UserId}, transaction hash: {TransactionHash}.",
                userId, input.TxHash);
            throw new UserFriendlyException(
                $"Create Synchronize Transaction Job fail, transaction hash: {input.TxHash}.");
        }

        var syncTxEtoData =
            _objectMapper.Map<SynchronizeTxJobGrainDto, SynchronizeTransactionInfoEto>(result.Data);
        syncTxEtoData.UserId = userId;
        syncTxEtoData.Id = input.TxHash;
        syncTxEtoData.LastModifyTime = TimeStampHelper.GetTimeStampInMilliseconds();

        await _distributedEventBus.PublishAsync(syncTxEtoData);

        return new SendNFTSyncResponseDto();
    }

    [ExceptionHandler(typeof(Exception),
        Message =
            "SynchronizeAppService.SendSeedMainChainCreateSyncAsync An error occurred during the creation of the synchronous job",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new[] { "input" }
    )]
    public async Task SendSeedMainChainCreateSyncAsync(IndexerSeedMainChainChange input)
    {
        var userId = Guid.NewGuid();

        var syncTxData =
            await _synchronizeTransactionProvider.GetSynchronizeJobByTxHashAsync(null, input.TransactionId);

        if (syncTxData.TxHash != null && !string.IsNullOrEmpty(syncTxData.Status))
        {
            _logger.LogError(
                "SendSeedMainChainCreateSyncAsync error This transaction {TxHash} status {Status} had registry.",
                input.TransactionId,
                syncTxData.Status);
            return;
        }

        var synchronizeTransactionJobGrain = _clusterClient.GetGrain<ISynchronizeTxJobGrain>(input.TransactionId);

        var createSyncTransJobDto =
            _objectMapper.Map<IndexerSeedMainChainChange, CreateSynchronizeTransactionJobGrainDto>(input);
        createSyncTransJobDto.Status = SynchronizeTransactionJobStatus.TokenCreating;

        _logger.LogInformation("TxHash: {TxHash} Synchronize-Transaction-Job will be created",
            createSyncTransJobDto.TxHash);
        var result =
            await synchronizeTransactionJobGrain.CreateSynchronizeTransactionJobAsync(createSyncTransJobDto);

        if (!result.Success)
        {
            _logger.LogError(
                "Create Synchronize Transaction Job fail, user id: {UserId}, transaction hash: {TransactionHash}.",
                userId, input.TransactionId);
            throw new UserFriendlyException(
                $"Create Synchronize Transaction Job fail, transaction hash: {input.TransactionId}.");
        }

        var syncTxEtoData =
            _objectMapper.Map<SynchronizeTxJobGrainDto, SynchronizeTransactionInfoEto>(result.Data);
        syncTxEtoData.UserId = userId;
        syncTxEtoData.Id = input.TransactionId;
        syncTxEtoData.LastModifyTime = TimeStampHelper.GetTimeStampInMilliseconds();

        await _distributedEventBus.PublishAsync(syncTxEtoData);
    }


    private string DetermineTheSynchronizationJobInitStatus(string symbol)
    {
        switch (NFTHelper.GetCreateInputSymbolType(symbol))
        {
            case SymbolType.NftCollection:
            case SymbolType.Token:
                return SynchronizeTransactionJobStatus.ProxyAccountsAndTokenCreating;
            case SymbolType.Nft:
                return SynchronizeTransactionJobStatus.TokenCreating;
            case SymbolType.Unknown:
            default:
                throw new UserFriendlyException($"Invalid symbol input {symbol}");
        }
    }
}