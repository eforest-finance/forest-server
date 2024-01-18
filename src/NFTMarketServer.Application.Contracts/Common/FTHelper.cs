using System;

namespace NFTMarketServer.Common;

public class FTHelper
{
    public static decimal GetRealELFAmount(decimal amount)
    {
        return ToPrice(amount, 8);
    }

    private static decimal ToPrice(decimal amount, int decimals)
    {
        return amount / (decimal)Math.Pow(10, decimals);
    }
}