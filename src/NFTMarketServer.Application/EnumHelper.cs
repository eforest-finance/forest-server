using System;
using System.Runtime.Serialization;

namespace NFTMarketServer;

public static class EnumHelper
{
    public static TEnum ToEnum<TEnum>(this string value) where TEnum : struct, Enum
    {
        foreach (var field in typeof(TEnum).GetFields())
        {
            if (Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute)) is EnumMemberAttribute attribute)
            {
                if (attribute.Value == value)
                {
                    return (TEnum)field.GetValue(null);
                }
            }
            else if (field.Name == value)
            {
                return (TEnum)field.GetValue(null);
            }
        }

        throw new ArgumentException($"No matching enum value found for "+nameof(value)+" . EnumType:"+typeof(TEnum).Name);
    }
}