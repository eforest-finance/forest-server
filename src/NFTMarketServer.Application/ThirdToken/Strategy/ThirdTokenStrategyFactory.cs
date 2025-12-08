using System;
using System.Collections.Generic;
using NFTMarketServer.ThirdToken.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.ThirdToken.Strategy;

public class ThirdTokenStrategyFactory : ISingletonDependency
{
    private readonly Dictionary<ThirdTokenType, IThirdTokenStrategy> _initMap = new();
    private readonly IEnumerable<IThirdTokenStrategy> _thirdTokenStrategies;

    public ThirdTokenStrategyFactory(IEnumerable<IThirdTokenStrategy> thirdTokenStrategies)
    {
        _thirdTokenStrategies = thirdTokenStrategies;
        InitHandlers();
    }

    private void InitHandlers()
    {
        if (!_initMap.IsNullOrEmpty())
        {
            return;
        }

        foreach (var strategy in _thirdTokenStrategies)
        {
            _initMap.Add(strategy.GetThirdTokenType(), strategy);
        }
    }

    public IThirdTokenStrategy GetStrategy(ThirdTokenType type)
    {
        return _initMap.GetValueOrDefault(type) ??
               throw new NotSupportedException($"Unsupported ThirdTokenType: {type}");
    }
}