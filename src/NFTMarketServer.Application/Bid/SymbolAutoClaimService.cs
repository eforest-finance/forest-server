using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Dealer.ContractInvoker;
using NFTMarketServer.Dealer.Dtos;

namespace NFTMarketServer.Bid;

public class SymbolAutoClaimService : NFTMarketServerAppService, ISymbolAutoClaimService

{
    private const long Wait_Time = 2;
    private const int Period = 3500;
    private const int QueryCount = 100;
    private const int MaxCount = 100;
    private readonly ILogger<SymbolAutoClaimService> _logger;
    private readonly IBidAppService _bidAppService;
    private readonly IContractInvokerFactory _contractInvokerFactory;


    public SymbolAutoClaimService(ILogger<SymbolAutoClaimService> logger,
        IBidAppService bidAppService, IContractInvokerFactory contractInvokerFactory)
    {
        _logger = logger;
        _bidAppService = bidAppService;
        _contractInvokerFactory = contractInvokerFactory;
    }

    public async Task SyncSymbolClaimAsync()
    {
        var requestDto = new GetAuctionInfoRequestDto
        {
            SkipCount = 0,
            MaxResultCount = MaxCount
        };
        var auctionInfoDtoList = await _bidAppService.GetUnFinishedSymbolAuctionInfoListAsync(requestDto);

        while (auctionInfoDtoList != null && auctionInfoDtoList.Count > 0)
        {
            foreach (var auctionInfoDto in auctionInfoDtoList)
            {
                var currentOffset = DateTimeOffset.Now;
                var currentTime = currentOffset.ToUnixTimeSeconds();
                if (auctionInfoDto.EndTime <= 0)
                {
                    continue;
                }
                if (currentTime > auctionInfoDto.EndTime + Wait_Time)
                {
                    auctionInfoDto.FinishIdentifier = AuctionFinishType.Finishing;
                    await _bidAppService.UpdateSymbolAuctionInfoAsync(auctionInfoDto);

                    _logger.LogDebug("Auto Claim Dealer  item {auctionId}", auctionInfoDto.Id);
                    await _contractInvokerFactory.Invoker(BizType.AuctionClaim.ToString()).InvokeAsync(auctionInfoDto);
                }
            }

            requestDto.SkipCount += QueryCount;
            auctionInfoDtoList = await _bidAppService.GetUnFinishedSymbolAuctionInfoListAsync(requestDto);
            _logger.LogDebug("SymbolClaimEventSyncWorker DoWorkAsync queryList count: {count}",
                auctionInfoDtoList.Count);
        }
    }
}