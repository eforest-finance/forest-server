using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Provider;
using NFTMarketServer.Inscription;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using NFTMarketServer.Basic;
using NFTMarketServer.Chains;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.Inscription;
using NFTMarketServer.Helper;
using Org.BouncyCastle.Utilities.Encoders;
using Orleans;

namespace NFTMarketServer.Dealer.ContractInvoker.Inscription;

public class InscriptionCollectionCrossChainCreateInvoker : AbstractContractInvoker
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<InscriptionCollectionCrossChainCreateInvoker> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IOptionsMonitor<SynchronizeTransactionJobOptions> _optionsMonitor;
    private readonly CrossChainProvider _crossChainProvider;
    private readonly IChainAppService _chainAppService;
    private readonly InscriptionItemCrossChainCreateInvoker _next;
    private readonly IClusterClient _clusterClient;

    public InscriptionCollectionCrossChainCreateInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, IObjectMapper objectMapper,
        IOptionsMonitor<SynchronizeTransactionJobOptions> optionsMonitor,
        CrossChainProvider crossChainProvider,
        IChainAppService chainAppService,
        InscriptionItemCrossChainCreateInvoker next,
        IClusterClient clusterClient,
        ILogger<InscriptionCollectionCrossChainCreateInvoker> logger) : base(distributedEventBus,
        contractInvokeProvider, objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _crossChainProvider = crossChainProvider;
        _chainAppService = chainAppService;
        _next = next;
        _clusterClient = clusterClient;
    }

    public override string BizType()
    {
        return Dtos.BizType.InscriptionCollectionCrossChainCreate.ToString();
    }

    public override async Task<ContractParamDto> AdaptToContractParamAsync<TCrossChainDto>(TCrossChainDto crossChainDto)
    {
      
        AssertHelper.NotNull(crossChainDto, "crossChainDto empty");
        AssertHelper.NotNull(crossChainDto is CrossChainCreateDto, "Invalid crossChainDto type");
        var crossChain = crossChainDto as CrossChainCreateDto;
        _logger.LogInformation(
            "InscriptionCollectionCrossChainCreate AdaptToContractParamAsync  begin Symbol:{Symbol}", crossChain.Symbol);
        var contractParamDto = new ContractParamDto
        {
            BizId = crossChain.Symbol,
            BizType = BizType(),
            ChainId = crossChain.ToChainId,
            ContractName = DealerContractType.TokenContractName,
            ContractMethod = DealerContractType.CrossChainCreateToken,
            Sender = DealerContractType.TokenContractAccount,
            BizData = new CrossChainCreateTokenInput
            {
                FromChainId = ChainHelper.ConvertBase58ToChainId(crossChain.FromChainId),
                ParentChainHeight = crossChain.ParentChainHeight,
                TransactionBytes = crossChain.TransactionBytes,
                MerklePath = ContractConvertHelper.ConvertMerklePath(crossChain.MerklePathDto)
            }.ToByteString().ToBase64()
        };
        return contractParamDto;
    }

    public override async Task ResultCallbackAsync(string symbol, bool invokeSuccess, TransactionResultDto result,
        string rawTransaction)
    {
        _logger.LogInformation(
            "InscriptionCollectionCrossChainCreate ResultCallbackAsync symbol:{Symbol} invokeSuccess:{InvokeSuccess} TransactionId:{TransactionId} Error:{Error}",
            symbol, invokeSuccess, result.TransactionId, result.Error);
        if (invokeSuccess || result.Error.Contains(CommonConstant.TokenExist))
        {
            var tick = NFTHelper.GetTick(symbol);
            var inscriptionItemCrossChainGrainResultDto = await _clusterClient
                .GetGrain<IInscriptionItemCrossChainGrain>(tick)
                .SaveCollectionCreated(true);
            var inscriptionItemCrossChainGrainDto = inscriptionItemCrossChainGrainResultDto.Data;
            await _crossChainProvider.WaitingHeightAsync(inscriptionItemCrossChainGrainDto.ParentChainHeight);
            _logger.LogInformation(
                "indexing success symbol:{Symbol}  TransactionId:{TransactionId}",
                inscriptionItemCrossChainGrainDto.Symbol, inscriptionItemCrossChainGrainDto.TransactionId);
            MerklePathDto itemMerklePathDto =
                await _crossChainProvider.GetMerklePathDtoAsync(DealerContractType.MainChainId,
                    inscriptionItemCrossChainGrainDto.TransactionId);
            await _next.InvokeAsync(
                new CrossChainCreateDto
                {
                    FromChainId = inscriptionItemCrossChainGrainDto.FromChainId,
                    ToChainId = inscriptionItemCrossChainGrainDto.ToChainId,
                    Symbol = inscriptionItemCrossChainGrainDto.Symbol,
                    ParentChainHeight = inscriptionItemCrossChainGrainDto.ParentChainHeight,
                    MerklePathDto = itemMerklePathDto,
                    TransactionBytes =
                        ByteString.CopyFrom(Base64.Decode(inscriptionItemCrossChainGrainDto.ValidateRawTransaction))
                });
            return;
        }

        // fail retry
        await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
        var transaction = Transaction.Parser.ParseFrom(Base64.Decode(rawTransaction));
        var crossChainCreateTokenInput = CrossChainCreateTokenInput.Parser.ParseFrom(transaction.Params);
        var originTransaction =
                Transaction.Parser.ParseFrom(crossChainCreateTokenInput.TransactionBytes);
        var validateTokenInfoExistsInput = ValidateTokenInfoExistsInput.Parser.ParseFrom(originTransaction.Params);
        var originTransactionId = originTransaction.GetHash().ToHex();
        //get newest blockHeight
        var transactionResult =
            await _crossChainProvider.GetTransactionResultAsync(DealerContractType.MainChainId, originTransactionId);
        await _crossChainProvider.WaitingHeightAsync(transactionResult.Transaction.RefBlockNumber);
        _logger.LogInformation(
            "indexing success symbol:{Symbol}  TransactionId:{TransactionId}",
            symbol, result.TransactionId);

        MerklePathDto collectionMerklePathDto =
            await _crossChainProvider.GetMerklePathDtoAsync(DealerContractType.MainChainId, result.TransactionId);
        var sideChainId = await _chainAppService.GetChainIdAsync(1);
        CrossChainCreateDto crossChainCreateDto = new CrossChainCreateDto()
            {
                FromChainId = DealerContractType.MainChainId,
                ToChainId = sideChainId,
                Symbol = validateTokenInfoExistsInput.Symbol,
                ParentChainHeight = transactionResult.Transaction.RefBlockNumber,
                MerklePathDto = collectionMerklePathDto,
                TransactionBytes = crossChainCreateTokenInput.TransactionBytes
            };
            await InvokeAsync(crossChainCreateDto);
        }
    
}