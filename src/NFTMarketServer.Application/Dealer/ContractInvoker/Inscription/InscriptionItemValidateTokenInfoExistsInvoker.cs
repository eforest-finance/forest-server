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
using NFTMarketServer.Grains.Grain.Inscription;
using NFTMarketServer.Helper;
using NFTMarketServer.Inscription;
using Org.BouncyCastle.Utilities.Encoders;
using Orleans;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Dealer.ContractInvoker.Inscription;

public class InscriptionItemValidateTokenInfoExistsInvoker : AbstractContractInvoker
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<InscriptionItemValidateTokenInfoExistsInvoker> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly CrossChainProvider _crossChainProvider;
    private readonly IOptionsMonitor<SynchronizeTransactionJobOptions> _optionsMonitor;
    private readonly IChainAppService _chainAppService;
    private readonly IClusterClient _clusterClient;


    public InscriptionItemValidateTokenInfoExistsInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, IObjectMapper objectMapper,
        CrossChainProvider crossChainProvider, 
        IOptionsMonitor<SynchronizeTransactionJobOptions> optionsMonitor,
        IChainAppService chainAppService,
        IClusterClient clusterClient,
        ILogger<InscriptionItemValidateTokenInfoExistsInvoker> logger) : base(distributedEventBus,
        contractInvokeProvider, objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _logger = logger;
        _crossChainProvider = crossChainProvider;
        _optionsMonitor = optionsMonitor;
        _chainAppService = chainAppService;
        _clusterClient = clusterClient;
    }

    public override string BizType()
    {
        return Dtos.BizType.InscriptionItemValidateTokenInfoExists.ToString();
    }

    public override async Task<ContractParamDto> AdaptToContractParamAsync<TInscriptionDto>(TInscriptionDto inscriptionDto)
    {
       
        AssertHelper.NotNull(inscriptionDto, "inscriptionDto empty");
        AssertHelper.NotNull(inscriptionDto is InscriptionDto, "Invalid inscriptionDto type");
        var inscription = inscriptionDto as InscriptionDto;
        var externalInfo = ContractConvertHelper.ConvertExternalInfoMapField(inscription.ItemExternalInfo);
        var symbol = NFTHelper.ConvertItemSymbol(inscription.Tick);
        _logger.LogInformation(
            "InscriptionItemValidateTokenInfoExists AdaptToContractParamAsync begin symbol {Symbol}", symbol);
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
            "InscriptionItemValidateTokenInfoExists ResultCallbackAsync symbol:{Symbol} invokeSuccess:{InvokeSuccess} TransactionId:{TransactionId},error:{Error}",
            symbol, invokeSuccess, result.TransactionId, result.Error);
        if (invokeSuccess)
        {
            var tick = NFTHelper.GetTick(symbol);
            var crossChainId = await _chainAppService.GetChainIdAsync(1);
            await _clusterClient.GetGrain<IInscriptionItemCrossChainGrain>(tick)
                .SaveItemCrossChainTransactionAsync(
                    new InscriptionItemCrossChainGrainDto
                    {
                        Id = tick,
                        ValidateRawTransaction = rawTransaction,
                        FromChainId = DealerContractType.MainChainId,
                        ToChainId = crossChainId,
                        Symbol = symbol,
                        ParentChainHeight = result.BlockNumber,
                        TransactionId = result.TransactionId,
                        IsCollectionCreated = false
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