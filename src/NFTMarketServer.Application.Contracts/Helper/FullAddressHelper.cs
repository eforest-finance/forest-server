using System;

namespace NFTMarketServer.Helper;

public static class FullAddressHelper
{
    private const string FullAddressPrefix = "ELF";
    private const char FullAddressSeparator = '_';
    
    public static string ToFullAddress(string address, string chainId)
    {
        if (address.IsNullOrEmpty() || chainId.IsNullOrEmpty())
            return address; 
        var parts = address.Split(FullAddressSeparator);
        if (parts.Length < 3)
            return string.Join(FullAddressSeparator, FullAddressPrefix, parts[parts.Length - 1], chainId);

        if (address.EndsWith(chainId))
            return address;
        
        return  string.Join(FullAddressSeparator, parts[0], parts[1], chainId);
    }

    public static string ToShortAddress(string address)
    {
        if (address.IsNullOrEmpty()) return address; 
        var parts = address.Split(FullAddressSeparator);
        return parts.Length < 3 ? parts[parts.Length - 1] : parts[1];
    }
    
    
}