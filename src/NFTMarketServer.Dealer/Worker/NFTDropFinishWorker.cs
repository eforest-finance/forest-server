using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Options;
using NFTMarketServer.Dealer.Provider;
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

    private readonly IOptionsMonitor<ChainOption> _chainOption;
    private readonly INFTDropInfoProvider _dropInfoProvider;
    private IContractProvider _contractProvider;
    private readonly IContractInvokerFactory _contractInvokerFactory;
    
    public NFTDropFinishWorker(
        ILogger<NFTDropFinishWorker> logger,
        INFTDropInfoProvider dropInfoProvider,
        IContractProvider contractProvider, 
        IContractInvokerFactory contractInvokerFactory,
        IOptionsMonitor<ChainOption> chainOption)
    {
        _logger = logger;
        _dropInfoProvider = dropInfoProvider;
        _contractProvider = contractProvider;
        _contractInvokerFactory = contractInvokerFactory;
        _chainOption = chainOption;
    }

    public async Task CheckExpireDrop()
    {
        _logger.LogInformation("CheckExpireDropWorker start...");

        var expireDropList = await _dropInfoProvider.GetExpireNFTDropListAsync();
        foreach (var dropInfo in expireDropList.DropInfoIndexList)
        {
            var index = dropInfo.MaxIndex;
            _logger.LogInformation("CheckExpireDropWorker drop index: {index}", index);
            while (index > 0)
            {
                await _contractInvokerFactory.Invoker(BizType.NFTDropFinish.ToString()).InvokeAsync(
                    new NFTDropFinishBizDto
                    {
                        DropId = dropInfo.DropId
                    });
                index -= 1;
            }
        }
    }

}