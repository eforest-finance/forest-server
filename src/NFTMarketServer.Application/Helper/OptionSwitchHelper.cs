using Microsoft.Extensions.Options;
using NFTMarketServer.Options;

namespace NFTMarketServer.Helper;

public class OptionSwitchHelper
{
    private static IOptionsMonitor<FuzzySearchOptions> _fuzzySearchOptions;
    
    
    public OptionSwitchHelper(
        IOptionsMonitor<FuzzySearchOptions> fuzzySearchOptions)
    {
        _fuzzySearchOptions = fuzzySearchOptions;
    }

    public static bool GetFuzzySearchOptions()
    {
        var options = _fuzzySearchOptions.CurrentValue;
        if (options != null)
        {
            return options.FuzzySearchSwitch;
        }
        return false;
    }
}