using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using Forest.Inscription;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.Inscription.Client;
using NFTMarketServer.Grains.Grain.Options;
using NFTMarketServer.Grains.State.Inscription;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.Inscription;

public class InscriptionInscribeGrain : Grain<InscriptionInscribeState>, IInscriptionInscribeGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfClientProvider _inscriptionAElfClient;
    private readonly ILogger<InscriptionInscribeGrain> _logger;
    private readonly ContractOption _contractOption;
    private readonly IOptionsMonitor<InscriptionChainOptions> _chainOptionsMonitor;
    private const string DefaultMainChain = "AELF";

    public InscriptionInscribeGrain(IObjectMapper objectMapper,
        IAElfClientProvider inscriptionAElfClient,
        ILogger<InscriptionInscribeGrain> logger, IOptionsSnapshot<ContractOption> chainOption,
        IOptionsMonitor<InscriptionChainOptions> chainOptionsMonitor)
    {
        _objectMapper = objectMapper;
        _inscriptionAElfClient = inscriptionAElfClient;
        _logger = logger;
        _contractOption = chainOption.Value;
        _chainOptionsMonitor = chainOptionsMonitor;
    }

    public async Task<GrainResultDto<InscriptionInscribeGrainDto>> SaveInscription(string rawTransaction)
    {
        var transaction = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(rawTransaction));
        var input = new SendTransactionInput
        {
            RawTransaction = rawTransaction
        };
        var sendTransactionOutput =
            await _inscriptionAElfClient.GetClient(GetDefaultChainId()).SendTransactionAsync(input);
        var inscribedInput = InscribedInput.Parser.ParseFrom(transaction.Params);
        if (State.Id == Guid.Empty)
        {
            State.Id = this.GetPrimaryKey();
        }

        State.Tick = inscribedInput.Tick;
        State.Amount = inscribedInput.Amt;
        State.Status = TransactionState.Pending;
        State.ChainId = GetDefaultChainId();
        State.TransactionId = sendTransactionOutput.TransactionId;
        await WriteStateAsync();
        return new GrainResultDto<InscriptionInscribeGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<InscriptionInscribeState, InscriptionInscribeGrainDto>(State)
        };
    }

    public async Task UpdateInscriptionStatus()
    {
        var transactionResult = await _inscriptionAElfClient.GetClient(GetDefaultChainId())
            .GetTransactionResultAsync(State.TransactionId);
        var times = 0;
        while (transactionResult.Status == TransactionState.Pending && times < _contractOption.RetryTimes)
        {
            times++;
            await Task.Delay(_contractOption.RetryDelay);

            transactionResult = await _inscriptionAElfClient.GetClient(GetDefaultChainId())
                .GetTransactionResultAsync(State.TransactionId);
        }

        State.Status = transactionResult.Status;
        if (transactionResult.Status == TransactionState.Mined)
        {
            await GrainFactory.GetGrain<IInscriptionAmountGrain>(State.Tick).AddAmount(State.Tick, State.Amount);
        }

        await WriteStateAsync();
    }

    private string GetDefaultChainId()
    {
        var chainIds = _chainOptionsMonitor.CurrentValue.ChainInfos.Keys;
        foreach (var chainId in chainIds)
        {
            if (!chainId.Equals(DefaultMainChain))
            {
                return chainId;
            }
        }

        return DefaultMainChain;
    }
}