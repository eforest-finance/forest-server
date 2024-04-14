using System;

namespace NFTMarketServer.Common;

public static class PercentageCalculatorHelper
{

    private const int DecimalPlaces = 10000;
    
    public static decimal CalculatePercentage(decimal numerator, decimal denominator)
    {
        if (numerator == -1)
        {
            numerator = 0;
        }

        if (denominator == -1)
        {
            denominator = 0;
        }

        if (numerator == 0 && denominator == 0)
        {
            return 0;
        }

        if (denominator == 0)
        {
            return 101m;
        }
        
        var result = (numerator - denominator) / denominator;
        return Math.Truncate(result * DecimalPlaces) / DecimalPlaces;
    }
    
}