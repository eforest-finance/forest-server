using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

public class InscriptionCollectionValidateTokenInfoExistsInvoker : AbstractContractInvoker
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<InscriptionCollectionValidateTokenInfoExistsInvoker> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly CrossChainProvider _crossChainProvider;
    private readonly InscriptionCollectionCrossChainCreateInvoker _next;
    private readonly IOptionsMonitor<SynchronizeTransactionJobOptions> _optionsMonitor;
    private readonly IChainAppService _chainAppService;
    
    public InscriptionCollectionValidateTokenInfoExistsInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, IObjectMapper objectMapper,
        CrossChainProvider crossChainProvider,
        InscriptionCollectionCrossChainCreateInvoker next,
        IOptionsMonitor<SynchronizeTransactionJobOptions> optionsMonitor,
        IChainAppService chainAppService,
        ILogger<InscriptionCollectionValidateTokenInfoExistsInvoker> logger) : base(distributedEventBus,
        contractInvokeProvider, objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _logger = logger;
        _crossChainProvider = crossChainProvider;
        _next = next;
        _optionsMonitor = optionsMonitor;
        _chainAppService = chainAppService;
    }

    public override string BizType()
    {
        
        return Dtos.BizType.InscriptionCollectionValidateTokenInfoExists.ToString();
    }

    public override async Task<ContractParamDto> AdaptToContractParamAsync<TInscriptionDto>(TInscriptionDto inscriptionDto)
    {
     
        AssertHelper.NotNull(inscriptionDto, "inscriptionDto empty");
        AssertHelper.NotNull(inscriptionDto is InscriptionDto, "Invalid inscriptionDto type");
        var inscription = inscriptionDto as InscriptionDto;
        
        var externalInfo = ContractConvertHelper.ConvertExternalInfoMapField(inscription.CollectionExternalInfo);
        var symbol = NFTHelper.ConvertCollectionSymbol(inscription.Tick);
        _logger.LogInformation(
            "InscriptionCollectionValidateTokenInfoExists AdaptToContractParamAsync begin symbol {Symbol}", symbol);
        var crossChainId = await _chainAppService.GetChainIdAsync(1);
        var contractParamDto = new ContractParamDto
        {
            BizId = symbol,
            BizType = BizType(),
            ChainId = DealerContractType.MainChainId,
            CrossChainId = crossChainId,
            ContractName = DealerContractType.TokenContractName,
            ContractMethod = DealerContractType.ValidateTokenInfoExists,
            Sender = DealerContractType.TokenContractAccount,
            BizData = new ValidateTokenInfoExistsInput
            {
                Symbol = symbol,
                TokenName = inscription.Tick,
                TotalSupply = inscription.TotalSupply,
                Decimals = 0,
                Issuer = Address.FromBase58(inscription.Issuer),
                IsBurnable = true,
                IssueChainId = inscription.IssueChainId,
                Owner = Address.FromBase58(inscription.Owner),
                ExternalInfo = { externalInfo }
            }.ToByteString().ToBase64()
        };
        return contractParamDto;
    }

    public override async Task ResultCallbackAsync(string symbol, bool invokeSuccess, TransactionResultDto result,
        string rawTransaction)
    {
        _logger.LogInformation(
            "InscriptionCollectionValidateTokenInfoExists ResultCallbackAsync symbol:{Symbol} invokeSuccess:{InvokeSuccess} TransactionId:{TransactionId},Error:{Error}",
            symbol, invokeSuccess, result.TransactionId, result.Error);
       
        if (invokeSuccess)
        {
            await _crossChainProvider.WaitingHeightAsync(result.Transaction.RefBlockNumber);
            _logger.LogInformation(
                "indexing success symbol:{Symbol}  TransactionId:{TransactionId}",
                symbol, result.TransactionId);
            var crossChainId = await _chainAppService.GetChainIdAsync(1);
            MerklePathDto newMerklePathDto =
                await _crossChainProvider.GetMerklePathDtoAsync(DealerContractType.MainChainId, result.TransactionId);
            await _next.InvokeAsync(
                    new CrossChainCreateDto
                    {
                        FromChainId = DealerContractType.MainChainId,
                        ToChainId = crossChainId,
                        Symbol = symbol,
                        ParentChainHeight = result.BlockNumber,
                        MerklePathDto = newMerklePathDto,
                        TransactionBytes = ByteString.CopyFrom(Base64.Decode(rawTransaction))
                    });
            return;
        }

        await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
        var originTransaction =
                Transaction.Parser.ParseFrom(Base64.Decode(rawTransaction));
        var validateTokenInfoExistsInput = ValidateTokenInfoExistsInput.Parser.ParseFrom(originTransaction.Params);
        // fail retry
        await InvokeAsync(new InscriptionDto
            {
                Tick = validateTokenInfoExistsInput.TokenName,
                TotalSupply = validateTokenInfoExistsInput.TotalSupply,
                Issuer = validateTokenInfoExistsInput.Issuer.ToBase58(),
                IssueChainId = validateTokenInfoExistsInput.IssueChainId,
                CollectionExternalInfo =
                    ContractConvertHelper.ConvertExternalInfoDtoList(validateTokenInfoExistsInput.ExternalInfo),
                Owner = validateTokenInfoExistsInput.Owner.ToBase58()
            });
    }
}