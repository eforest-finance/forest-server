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
    
    public static string ToEnumString<TEnum>(this TEnum enumValue) where TEnum : struct, Enum
    {
        var field = typeof(TEnum).GetField(enumValue.ToString());
        if (field != null)
        {
            if (Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute)) is EnumMemberAttribute attribute)
            {
                return attribute.Value;
            }
            else
            {
                return field.Name;
            }
        }

        throw new ArgumentException($"No matching string value found for enum {nameof(enumValue)}. EnumType: {typeof(TEnum).Name}");
    }
    
    public static int GetIndex(this Enum value)
    {
        var enumType = value.GetType();
        var name = Enum.GetName(enumType, value);
        var names = Enum.GetNames(enumType);
        return Array.IndexOf(names, name);
    }
}