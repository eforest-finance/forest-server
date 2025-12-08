using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.NFT.Provider;
using Volo.Abp.DependencyInjection;
using NFTMarketServer.Dealer.ContractInvoker;

namespace NFTMarketServer.Dealer.Worker;

public interface INFTDropFinishWorker
{
    Task CheckExpireDrop();
}

public class NFTDropFinishWorker : INFTDropFinishWorker, ISingletonDependency
{
    private const string LockKeyPrefix = "ContractInvokerProvider:";

    private readonly ILogger<NFTDropFinishWorker> _logger;
    
    private readonly INFTDropInfoProvider _dropInfoProvider;
    private readonly IContractInvokerFactory _contractInvokerFactory;
    
    public NFTDropFinishWorker(
        ILogger<NFTDropFinishWorker> logger,
        INFTDropInfoProvider dropInfoProvider,
        IContractInvokerFactory contractInvokerFactory)
    {
        _logger = logger;
        _dropInfoProvider = dropInfoProvider;
        _contractInvokerFactory = contractInvokerFactory;
    }

    public async Task CheckExpireDrop()
    {
        _logger.LogInformation("CheckExpireDropWorker start...");

        var expireDropList = await _dropInfoProvider.GetExpireNFTDropListAsync();
        foreach (var dropInfo in expireDropList.DropInfoIndexList)
        {
            var maxIndex = dropInfo.MaxIndex;
            var index = 1;
            _logger.LogInformation("CheckExpireDropWorker drop index: {index}", index);
            while (index <= maxIndex)
            {
                await _contractInvokerFactory.Invoker(BizType.NFTDropFinish.ToString()).InvokeAsync(
                    new NFTDropFinishBizDto
                    {
                        DropId = dropInfo.DropId,
                        Index = index
                    });
                index += 1;
            }
        }
    }

}