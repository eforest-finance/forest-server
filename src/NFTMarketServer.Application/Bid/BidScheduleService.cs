using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Provider;

namespace NFTMarketServer.Bid;

public class BidScheduleService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IBidAppService _bidAppService;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IChainAppService _chainAppService;

    public BidScheduleService(ILogger<BidScheduleService> logger, IBidAppService bidAppService,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _bidAppService = bidAppService;
        _graphQlProvider = graphQlProvider;
        _chainAppService = chainAppService;
    }


    public override async Task<long> SyncIndexerRecordsAsync(string chainId,long lastEndHeight, long newIndexHeight)
    {
        var queryList = await _graphQlProvider.GetSyncSymbolBidRecordsAsync(chainId, lastEndHeight, 0);
        _logger.LogInformation("GetSyncSymbolBidRecordsAsync queryList count: {count}", queryList.Count);
        
        long blockHeight = -1;

        if (CollectionUtilities.IsNullOrEmpty(queryList))
        {
            return 0;
        }

        _logger.LogInformation("bidInfo Task start time:{time}", DateTime.UtcNow.ToString());


        foreach (var queryDto in queryList)
        {
            var dealDataStart = DateTime.UtcNow;
            _logger.LogInformation(" bidInfo query by TransactionHash start time:{time} seedSymbol:{seedSymbol},amount:{amount},newIndexHeight:{newIndexHeight}",
                dealDataStart.ToString(), queryDto.SeedSymbol, queryDto.PriceAmount, newIndexHeight);
            blockHeight = Math.Max(blockHeight, queryDto.BlockHeight);

            var bidInfoDto = await _bidAppService.GetSymbolBidInfoAsync(queryDto.SeedSymbol, queryDto.TransactionHash);
            var dealDataEnd = DateTime.UtcNow;
            _logger.LogInformation(" bidInfo query by TransactionHash end time:{time} seedSymbol:{seedSymbol},amount:{amount}",
                dealDataEnd.ToString(), queryDto.SeedSymbol, queryDto.PriceAmount);
            if (bidInfoDto != null)
            {
                continue;
            }

            _logger.LogInformation(" bidInfo start push time:{time} seedSymbol:{seedSymbol},amount:{amount}",
                DateTime.UtcNow.ToString(), queryDto.SeedSymbol, queryDto.PriceAmount);
            await _bidAppService.AddBidInfoListAsync(queryDto);
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
        return BusinessQueryChainType.SymbolBid;
    }
}