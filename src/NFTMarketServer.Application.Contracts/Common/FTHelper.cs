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
    
    public static long GetIntegerDivision(long number, int decimals)
    {
        if (decimals == CommonConstant.IntZero || number == CommonConstant.IntZero)
        {
            return number;
        }

        var divisor = (long)Math.Pow(CommonConstant.IntTen, decimals);
        return number / divisor;
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

    public static string BuildIpfsUrl(string previewImage)
    {
        if (!string.IsNullOrEmpty(previewImage) && previewImage?.IndexOf(CommonConstant.MetadataImageUriKeyPre) >= 0)
        {
            return CommonConstant.ImageIpfsUrlPre +
                   previewImage.Substring(CommonConstant.MetadataImageUriKeyPre.Length);
        }

        return previewImage;
    }
}