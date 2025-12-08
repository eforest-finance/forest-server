using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Dealer.ContractInvoker;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Provider;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Inscription;

[RemoteService(IsEnabled = false)]
public class InscriptionCrossChainScheduleService : ScheduleSyncDataService, ITransientDependency
{
    private readonly IChainAppService _chainAppService;
    private readonly IContractInvokerFactory _contractInvokerFactory;
    private readonly ILogger<InscriptionCrossChainScheduleService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IOptionsMonitor<SynchronizeTransactionJobOptions> _optionsMonitor;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private readonly int _heightExpireMinutes = 10;

    public InscriptionCrossChainScheduleService(ILogger<InscriptionCrossChainScheduleService> logger,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        IContractInvokerFactory contractInvokerFactory,
        IOptionsMonitor<SynchronizeTransactionJobOptions> optionsMonitor,
        IDistributedCache<List<string>> distributedCache
    ) : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _chainAppService = chainAppService;
        _contractInvokerFactory = contractInvokerFactory;
        _optionsMonitor = optionsMonitor;
        _distributedCache = distributedCache;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var beginBlockHeight = _optionsMonitor.CurrentValue.BeginHeight;
        if (beginBlockHeight > newIndexHeight)
        {
            return newIndexHeight;
        }

        var inscriptionDtosCount = 0;
        var maxResultCount = 900; // maxResultCount must less 1000
        do
        {
            _logger.LogInformation(
                "SyncIndexerRecordsAsync LastEndHeight {LastEndHeight},NewIndexHeight{NewIndexHeight}",
                lastEndHeight, newIndexHeight);
            List<InscriptionDto> inscriptionDtos =
                await _graphQlProvider.GetIndexInscriptionAsync(chainId, lastEndHeight, newIndexHeight, 0,
                    maxResultCount);
            if (inscriptionDtos.IsNullOrEmpty())
            {
                break;
            }

            List<string> tickList = await _distributedCache.GetAsync(GetInscriptionHeightCacheKey(lastEndHeight));
            foreach (var inscription in inscriptionDtos)
            {
                if (tickList != null && tickList.Contains(inscription.Tick))
                {
                    continue;
                }

                lastEndHeight = Math.Max(lastEndHeight, inscription.BlockHeight);
                _logger.LogInformation(
                    "SyncIndexerRecordsAsync inscription tick {Tick}", inscription.Tick);
                await _contractInvokerFactory
                    .Invoker(Dealer.Dtos.BizType.InscriptionCollectionValidateTokenInfoExists.ToString()).InvokeAsync(
                        inscription);
                await _contractInvokerFactory
                    .Invoker(Dealer.Dtos.BizType.InscriptionItemValidateTokenInfoExists.ToString())
                    .InvokeAsync(inscription);
            }

            tickList = inscriptionDtos.Where(obj => obj.BlockHeight == lastEndHeight)
                .Select(obj => obj.Tick)
                .ToList();
            await _distributedCache.SetAsync(GetInscriptionHeightCacheKey(lastEndHeight), tickList,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_heightExpireMinutes)
                });

            inscriptionDtosCount = inscriptionDtos.Count;
        } while (inscriptionDtosCount == maxResultCount);

        return lastEndHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(0);
        return new List<string> { chainId };

    }


    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.InscriptionCrossChain;
    }

    private string GetInscriptionHeightCacheKey(long blockHeight)
    {
        return $"InscriptionHeight:{blockHeight}";
    }
}