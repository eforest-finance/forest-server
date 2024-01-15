using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Basic;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Provider;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Helper;
using NFTMarketServer.Inscription;
using Org.BouncyCastle.Utilities.Encoders;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Dealer.ContractInvoker.Inscription;

public class InscriptionItemCrossChainCreateInvoker : AbstractContractInvoker
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<InscriptionItemCrossChainCreateInvoker> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly InscriptionIssueInvoker _next;
    private readonly IOptionsMonitor<SynchronizeTransactionJobOptions> _optionsMonitor;
    private readonly CrossChainProvider _crossChainProvider;
    private readonly IChainAppService _chainAppService;
    public InscriptionItemCrossChainCreateInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, IObjectMapper objectMapper,
        InscriptionIssueInvoker next,
        IOptionsMonitor<SynchronizeTransactionJobOptions> optionsMonitor,
        CrossChainProvider crossChainProvider,
        IChainAppService chainAppService,
        ILogger<InscriptionItemCrossChainCreateInvoker> logger) : base(distributedEventBus,
        contractInvokeProvider, objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _logger = logger;
        _next = next;
        _optionsMonitor = optionsMonitor;
        _crossChainProvider = crossChainProvider;
        _chainAppService = chainAppService;
    }

    public override string BizType()
    {
        return Dtos.BizType.InscriptionItemCrossChainCreate.ToString();
    }

    public override async Task<ContractParamDto> AdaptToContractParamAsync<TCrossChainDto>(TCrossChainDto crossChainDto)
    {
      
        AssertHelper.NotNull(crossChainDto, "crossChainDto empty");
        AssertHelper.NotNull(crossChainDto is CrossChainCreateDto, "Invalid crossChainDto type");
        var crossChain = crossChainDto as CrossChainCreateDto;
        _logger.LogInformation(
            "InscriptionItemCrossChainCreate AdaptToContractParamAsync begin symbol {Symbol}", crossChain.Symbol);
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
            "InscriptionItemCrossChainCreate ResultCallbackAsync symbol:{symbol} invokeSuccess:{invokeSuccess} TransactionId:{TransactionId},error:{Error}",
            symbol, invokeSuccess, result.TransactionId, result.Error);
        if (invokeSuccess || result.Error.Contains(CommonConstant.TokenExist))
        {
            await _next.InvokeAsync(NFTHelper.GetTick(symbol));
            return;
        }

        await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
        var transaction = Transaction.Parser.ParseFrom(Base64.Decode(rawTransaction));
        var crossChainCreateTokenInput = CrossChainCreateTokenInput.Parser.ParseFrom(transaction.Params);
        var originTransaction =
                Transaction.Parser.ParseFrom(crossChainCreateTokenInput.TransactionBytes);
        var originTransactionId = originTransaction.GetHash().ToHex();
        //get newest blockHeight
        var transactionResult =
            await _crossChainProvider.GetTransactionResultAsync(DealerContractType.MainChainId, originTransactionId);

        await _crossChainProvider.WaitingHeightAsync(transactionResult.Transaction.RefBlockNumber);
        _logger.LogInformation(
            "indexing success symbol:{Symbol}  TransactionId:{TransactionId}",
            symbol, result.TransactionId);

        MerklePathDto newMerklePathDto =
            await _crossChainProvider.GetMerklePathDtoAsync(DealerContractType.MainChainId, result.TransactionId);
        var validateTokenInfoExistsInput = ValidateTokenInfoExistsInput.Parser.ParseFrom(originTransaction.Params);
        var crossChainId = await _chainAppService.GetChainIdAsync(1);
        // fail retry
        CrossChainCreateDto crossChainCreateDto = new CrossChainCreateDto()
        {
            FromChainId = DealerContractType.MainChainId,
            ToChainId = crossChainId,
            Symbol = validateTokenInfoExistsInput.Symbol,
            ParentChainHeight = transactionResult.Transaction.RefBlockNumber,
            MerklePathDto = newMerklePathDto,
            TransactionBytes = crossChainCreateTokenInput.TransactionBytes
        };

        await InvokeAsync(crossChainCreateDto);
    }
}