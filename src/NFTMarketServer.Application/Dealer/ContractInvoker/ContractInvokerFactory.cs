using System.Collections.Generic;
using System.Linq;
using NFTMarketServer.Common;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Dealer.ContractInvoker;


public interface IContractInvokerFactory
{
    IContractInvoker Invoker(string bizType);
}

public class ContractInvokerFactory : IContractInvokerFactory, ISingletonDependency
{
    private readonly Dictionary<string, IContractInvoker> _invokers;


    public ContractInvokerFactory(IEnumerable<IContractInvoker> invokers)
    {
        _invokers = invokers.ToDictionary(a => a.BizType(), a => a);
    }

    public IContractInvoker Invoker(string bizType)
    {
        var invoker = _invokers.GetValueOrDefault(bizType, null);
        AssertHelper.NotNull(invoker, "Adaptor of {bizType} not found", bizType);
        return invoker;
    }
}