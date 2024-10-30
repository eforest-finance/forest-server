using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using AElf;
using NFTMarketServer.Helper;

namespace NFTMarketServer;

public class IdGenerateHelper
{
    public static string ToSha256Hash(string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        using (SHA256 sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);

            StringBuilder builder = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
    public static string GetCollectionRelationId(string collectionId, string address)
    {
        return GetId(collectionId, address);
    }
    
    public static string GetAIImageId(string transactionId, string address, int number)
    {
        return GetId(transactionId, address, number);
    }
    
    public static string GetAiCreateId(string transactionId, string address)
    {
        return GetId(transactionId, address);
    }
    
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
    
    public static string GetNFTCollectionRarityId(string collectionSymbol, string rarity)
    {
        return GetId(collectionSymbol, rarity);
    }
    
    public static string GetSeedMainChainChangeId(string chainId, string symbol)
    {
        return GetId(chainId, symbol);
    }

    public static string GetCollectionIdSymbol(string collectionId)
    {
        int delimiterIndex = collectionId.IndexOf('-');

        if (delimiterIndex != -1)
        {
            return collectionId.Substring(delimiterIndex + 1);
        }

        return "";
    }

    public static string GetMessageActivityId(string bizId,string address)
    {
        return GetId(HashHelper.ComputeFrom(bizId).ToHex(), FullAddressHelper.ToShortAddress(address));
    }
    
    private static string TransferToHashStr(string str)
    {
        if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
        {
            return "";
        }

        return HashHelper.ComputeFrom(str).ToHex();
    }
    
    public static string GetTsmSeedSymbolId(string chainId,string seedOwnedSymbol)
    {
        return GetId(chainId, seedOwnedSymbol);
    }
}