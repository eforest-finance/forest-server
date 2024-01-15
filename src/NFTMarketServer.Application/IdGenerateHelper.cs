using System.Collections.Generic;
using AElf;

namespace NFTMarketServer;

public class IdGenerateHelper
{
    public static string GetId(params object[] inputs)
    {
        return inputs.JoinAsString("-");
    }

    public static string GetTokenInfoId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }

    public static string GetListingWhitelistPriceId(string nftInfoId, string address)
    {
        return GetId(nftInfoId, address);
    }

    public static string GetNFTInfoId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }

    public static string GetNFTCollectionId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }

    public static string GetUserBalanceId(string address, string chainId, string nftInfoId)
    {
        return GetId(address, chainId, nftInfoId);
    }

    public static string GetNftActivityId(string chainId, string symbol, string from, string to, string transactionId)
    {
        return GetId(chainId, symbol, from, to, transactionId);
    }

    public static string GetNftExtensionId(int chainId, string symbol)
    {
        return GetNftExtensionId(ChainHelper.ConvertChainIdToBase58((int)chainId), symbol);
    }

    public static string GetNftExtensionId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }
    
    public static string GetSeedPriceId(string tokenType, int symbolLength)
    {
        return GetId(tokenType, symbolLength);
    }
}