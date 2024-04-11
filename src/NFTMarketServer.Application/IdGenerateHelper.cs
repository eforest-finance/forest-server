using System;
using System.Collections.Generic;
using System.Text;
using AElf;
using AElf.Client.Proto;
using Hash = AElf.Types.Hash;

namespace NFTMarketServer;

public class IdGenerateHelper
{
    public static string GetHourlyCollectionTradeRecordId(string collectionId, string currentOrdinalStr)
    {
        return GetId(collectionId, currentOrdinalStr);
    }
    
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

    public static string GetNFTCollectionTraitKeyId(string nftCollectionSymbol, string traitKey)
    {
        return GetId(nftCollectionSymbol, TransferToHashStr(traitKey));
    }

    public static string GetNFTCollectionTraitPairsId(string nftCollectionSymbol, string traitKey, string traitValue)
    {
        return GetId(nftCollectionSymbol, TransferToHashStr(traitKey), TransferToHashStr(traitValue));
    }

    public static string GetNFTCollectionTraitGenerationId(string collectionSymbol, int Generation)
    {
        return GetId(collectionSymbol, Generation);
    }

    private static string TransferToHashStr(string str)
    {
        if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
        {
            return "";
        }

        return HashHelper.ComputeFrom(str).ToHex();
    }
}