using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Types;
using Forest.Contracts.Auction;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Provider;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Dealer.ContractInvoker;

public class AuctionAutoClaimInvoker : AbstractContractInvoker
{
    private readonly IObjectMapper _objectMapper;
    private readonly IBidAppService _bidAppService;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<AuctionAutoClaimInvoker> _logger;
    private readonly IChainAppService _chainAppService;
    public AuctionAutoClaimInvoker(IDistributedEventBus distributedEventBus,
        ContractInvokeProvider contractInvokeProvider, IObjectMapper objectMapper, IBidAppService bidAppService,
        ILogger<AuctionAutoClaimInvoker> logger,
        IChainAppService chainAppService) : base(distributedEventBus,
        contractInvokeProvider, objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _bidAppService = bidAppService;
        _logger = logger;
        _chainAppService = chainAppService;
    }

    public override string BizType()
    {
        return Dtos.BizType.AuctionClaim.ToString();
    }

    public override async Task<ContractParamDto> AdaptToContractParamAsync<TAuctionInfoDto>(TAuctionInfoDto auctionInfoDto)
    {
        AssertHelper.NotNull(auctionInfoDto, "auctionInfoDto empty");
        AssertHelper.NotNull(auctionInfoDto is AuctionInfoDto, "Invalid auctionInfoDto type");
        var auctionInfo = auctionInfoDto as AuctionInfoDto;
        AssertHelper.NotNull(auctionInfo, "auctionInfo empty");
        var sideChainId = await _chainAppService.GetChainIdAsync(1);
        var contractParamDto = new ContractParamDto
        {
            BizId = auctionInfo?.Id,
            BizType = BizType(),
            ChainId = sideChainId, 
            ContractName = DealerContractType.AuctionContractName,
            ContractMethod = DealerContractType.AuctionContractMethod,
            Sender = DealerContractType.AuctionAutoClaimAccount,
            BizData = new ClaimInput
            {
                AuctionId = Hash.LoadFromHex(auctionInfo?.Id)
            }.ToByteString().ToBase64()
        };
        return contractParamDto;
    }

    public override async Task ResultCallbackAsync(string auctionId, bool invokeSuccess, TransactionResultDto result,
        string rawTransaction)
    {
        _logger.LogInformation("AuctionAutoClaimInvoker ResultCallbackAsync auctionId:{auctionId} invokeSuccess:{invokeSuccess} TransactionId:{TransactionId}",
            auctionId, invokeSuccess, result.TransactionId);
        var symbolAuctionInfo = await _bidAppService.GetSymbolAuctionInfoByIdAsync(auctionId);
        symbolAuctionInfo.FinishIdentifier = invokeSuccess ? AuctionFinishType.Finished : AuctionFinishType.UnFinished;
        await _bidAppService.UpdateSymbolAuctionInfoAsync(symbolAuctionInfo);

    }
}