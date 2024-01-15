using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Dtos;

namespace NFTMarketServer.Helper;

public class EnumDescriptionHelper
{
    public static string GetEnumDescription(TokenCreatedExternalInfoEnum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute));
        return attribute?.Description ?? value.ToString();
    }
    
    public static string GetExtraInfoValue(IEnumerable<ExternalInfoDictionaryDto> externalInfo, TokenCreatedExternalInfoEnum keyEnum, string defaultValue = null)
    {
        var key = GetEnumDescription(keyEnum);
        return externalInfo.Where(kv => kv.Key.Equals(key))
            .Select(kv => kv.Value)
            .FirstOrDefault(defaultValue);
    }

}