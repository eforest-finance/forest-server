using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Provider;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Bid;

public class AuctionScheduleService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IBidAppService _bidAppService;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IChainAppService _chainAppService;
    private readonly IObjectMapper _objectMapper;

    public AuctionScheduleService(ILogger<AuctionScheduleService> logger, IBidAppService bidAppService,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService, IObjectMapper objectMapper)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _bidAppService = bidAppService;
        _graphQlProvider = graphQlProvider;
        _chainAppService = chainAppService;
        _objectMapper = objectMapper;
    }


    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var queryList = await _graphQlProvider.GetSyncSymbolAuctionRecordsAsync(chainId, lastEndHeight, 0);
        _logger.LogInformation("GetSyncSymbolAuctionRecordsAsync queryList count: {count}", queryList.Count);
        long blockHeight = -1;

        if (CollectionUtilities.IsNullOrEmpty(queryList))
        {
            return 0;
        }

        foreach (var queryDto in queryList)
        {
            blockHeight = Math.Max(blockHeight, queryDto.BlockHeight);
            var exitAuctionInfoDto = await _bidAppService.GetSymbolAuctionInfoByIdAndTransactionHashAsync(queryDto.Id, queryDto.TransactionHash);
            if (exitAuctionInfoDto != null)
            {
                continue;
            }
           
            await _bidAppService.UpdateSymbolAuctionInfoAsync(queryDto);
        }

        return blockHeight;
    }
    
    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(1);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.SymbolAuction;
    }
}