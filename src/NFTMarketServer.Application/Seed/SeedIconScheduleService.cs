using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NFTMarketServer.Bid;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.File;
using NFTMarketServer.Provider;


namespace NFTMarketServer.Seed;

public class SeedIconScheduleService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ISymbolIconAppService _symbolIconAppService;
    private readonly IChainAppService _chainAppService;

    public SeedIconScheduleService(ILogger<SeedIconScheduleService> logger, IGraphQLProvider graphQlProvider,
        ISymbolIconAppService symbolIconAppService, IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _symbolIconAppService = symbolIconAppService;
        _chainAppService = chainAppService;
    }


    public override async Task<long> SyncIndexerRecordsAsync(string chainId,long lastEndHeight, long newIndexHeight)
    {
        var queryList = await _graphQlProvider.GetSyncTsmSeedRecordsAsync(chainId, lastEndHeight, newIndexHeight);
        long blockHeight = -1;
        if (CollectionUtilities.IsNullOrEmpty(queryList))
        {
            return 0;
        }

        foreach (var queryDto in queryList)
        {
            blockHeight = Math.Max(blockHeight, queryDto.BlockHeight);
            var imageUrl = queryDto.SeedImage;
            if (imageUrl.IsNullOrEmpty())
            {
                continue;
            }

            var matchUrl = Regex.Match(imageUrl, @"([^/]+)\.svg");
            if (!matchUrl.Success)
            {
                continue;
            }
            if (queryDto.SeedSymbol.IsNullOrEmpty())
            {
                continue; 
            }
            await _symbolIconAppService.GetIconBySymbolAsync(queryDto.SeedSymbol, queryDto.Symbol);
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
        return BusinessQueryChainType.TsmSeedIcon;
    }
}