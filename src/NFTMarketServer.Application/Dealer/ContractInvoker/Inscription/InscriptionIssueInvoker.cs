using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Types;
using Forest.Inscription;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Basic;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Provider;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Inscription;
using Org.BouncyCastle.Utilities.Encoders;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Dealer.ContractInvoker.Inscription;

public class InscriptionIssueInvoker : AbstractContractInvoker
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<InscriptionIssueInvoker> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IOptionsMonitor<SynchronizeTransactionJobOptions> _optionsMonitor;
    private readonly IChainAppService _chainAppService;
    public InscriptionIssueInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, IObjectMapper objectMapper,
        IOptionsMonitor<SynchronizeTransactionJobOptions> optionsMonitor,
        IChainAppService chainAppService,
        ILogger<InscriptionIssueInvoker> logger) : base(distributedEventBus,
        contractInvokeProvider, objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _chainAppService = chainAppService;
    }

    public override string BizType()
    {
        return Dtos.BizType.InscriptionIssue.ToString();
    }

    public override async Task<ContractParamDto> AdaptToContractParamAsync<TIssue>(TIssue issue)
    {
        AssertHelper.NotNull(issue, "issue empty");
        AssertHelper.NotNull(issue is InscriptionDto, "Invalid issue type");
        var crossChainId = await _chainAppService.GetChainIdAsync(1);
        var tick = issue as string;
        _logger.LogInformation(
            "InscriptionIssue AdaptToContractParamAsync begin tick {Tick}", tick);
        var contractParamDto = new ContractParamDto
        {
            BizId = tick,
            BizType = BizType(),
            ChainId = crossChainId,
            ContractName = DealerContractType.InscriptionContractName,
            ContractMethod = DealerContractType.IssueInscription,
            Sender = DealerContractType.TokenContractAccount,
            BizData = new IssueInscriptionInput
            {
                Tick = tick
            }.ToByteString().ToBase64()
        };
        return contractParamDto;
    }

    public override async Task ResultCallbackAsync(string tick, bool invokeSuccess, TransactionResultDto result,
        string rawTransaction)
    {
        _logger.LogInformation(
            "InscriptionIssue ResultCallbackAsync symbol:{Tick} invokeSuccess:{InvokeSuccess} TransactionId:{TransactionId} error:{Error}",
            tick, invokeSuccess, result.TransactionId, result.Error);
        if (invokeSuccess || result.Error.Contains(CommonConstant.InscriptionIssueRepeat))
        {
            return;
        }

        await Task.Delay(_optionsMonitor.CurrentValue.CrossChainDelay);
        // fail retry
        await InvokeAsync(tick);
    }
}