using System;
using NFTMarketServer.Basic;

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

    public static bool IsGreaterThanEqualToOne(long supply, int decimalValue)
    {
        if (decimalValue == CommonConstant.IntZero)
        {
            return supply >= CommonConstant.IntOne;
        }

        if (supply == CommonConstant.IntZero)
        {
            return false;
        }

        var value = supply / (decimal)Math.Pow(CommonConstant.IntTen, decimalValue);
        return value >= CommonConstant.IntOne;
    }
}