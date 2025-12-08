using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Provider;

namespace NFTMarketServer.Seed;

public class UniqueSeedPriceRuleScheduleService: ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IChainAppService _chainAppService;
    private readonly ISeedPriceAppService _seedPriceAppService;
    public UniqueSeedPriceRuleScheduleService(ILogger<ScheduleSyncDataService> logger, IGraphQLProvider graphQlProvider, IChainAppService chainAppService, ISeedPriceAppService seedPriceAppService) : 
        base(logger, graphQlProvider,chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _chainAppService = chainAppService;
        _seedPriceAppService = seedPriceAppService;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var queryList = await _graphQlProvider.GetUniqueSeedPriceDtoRecordsAsync(chainId, lastEndHeight, 0);
        _logger.LogInformation("GetUniqueSeedPriceDtoRecordsAsync queryList count: {count}", queryList.Count);
        long blockHeight = -1;

        if (CollectionUtilities.IsNullOrEmpty(queryList))
        {
            return 0;
        }

        foreach (var queryDto in queryList)
        {
            blockHeight = Math.Max(blockHeight, queryDto.BlockHeight);
            await _seedPriceAppService.AddOrUpdateUniqueSeedPriceInfoAsync(queryDto);
        }

        return blockHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(0);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.UniquePriceRule;
    }
}