using System;

namespace NFTMarketServer.Common;

public static class PercentageCalculatorHelper
{
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

        if (denominator == 0)
        {
            return 101m;
        }

        var percentage = (numerator - denominator) / denominator;
        return Math.Round(percentage, 4);
    }

    public static decimal CalculatePercentage(long numerator, long denominator)
    {
        return CalculatePercentage(numerator, denominator);
    }
}