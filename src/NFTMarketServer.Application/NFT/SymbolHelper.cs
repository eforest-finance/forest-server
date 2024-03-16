using System;
using System.Text.RegularExpressions;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;

namespace NFTMarketServer.NFT;

public class SymbolHelper
{
    public const string MAIN_CHAIN_SYMBOL = "AELF";
    public const string MAIN_CHAIN_PREFIX = "MainChain ";
    public const string SIDE_CHAIN_PREFIX = "SideChain ";
    private const string Coin_Gecko_ELF = "ELF";
    private const string SEED = "SEED";
    private const string HYPHEN = "-";
    public const string SEED_COLLECTION = "SEED-0";
    public const string COLLECTION_SUFFIX = "-0";
    private const string FT_PATTERN = "^[A-Z]{1,10}$";
    private const string NFT_PREFIX_PATTERN = "^[A-Z]{1,28}$"; // NFT max length 30
    public const string NFTSymbolPattern = @"^.+-(?!0+$)[0-9]+$";

    private const string COMMON_NFT_PATTERN = "^.+-.+$";
    public static string GetHyphen()
    {
        return HYPHEN;
    }

    
    public static string CoinGeckoELF()
    {
        return Coin_Gecko_ELF;
    }

    public static string MainChainSymbol()
    {
        return MAIN_CHAIN_SYMBOL;
    }

    public static bool MatchSymbolPattern(string symbol)
    {
        return symbol.Match(FT_PATTERN);
    }
    public static bool MatchCommonSymbolPattern(string symbol)
    {
        return symbol.Match(COMMON_NFT_PATTERN);
    }

    public static bool MatchNFTPrefix(string symbol)
    {
        return symbol.Match(NFT_PREFIX_PATTERN);
    }
    
    

    public static int SubHyphenNumber(string symbol)
    {
        if (symbol.IsNullOrWhiteSpace())
        {
            return 0;
        }

        var lastHyphenIndex = symbol.LastIndexOf(HYPHEN);
        var numberStr = lastHyphenIndex == -1 ? "" : symbol.Substring(lastHyphenIndex + 1);
        int number;
        int.TryParse(numberStr, out number);
        return number;
    }

    public static bool CheckIsSeedCollection(string symbol)
    {
        return symbol.Equals(SEED_COLLECTION);
    }

    public static string TransferNFTIdToCollectionId(string nftId)
    {
        if (nftId.IsNullOrEmpty() || nftId.LastIndexOf(HYPHEN, StringComparison.Ordinal) < 0)
        {
            return "";
        }

        return nftId.Substring(0, nftId.LastIndexOf(HYPHEN, StringComparison.Ordinal)) + COLLECTION_SUFFIX;
    }

    public static string GainInscriptionInfoTick(string symbol)
    {
        return symbol.Substring(0,symbol.IndexOf(HYPHEN, StringComparison.Ordinal));
    }
    
    
    public static bool CheckChainIdIsMain(string chainId)
    {
        return chainId.Equals(MAIN_CHAIN_SYMBOL);
    }

    public static bool CheckSymbolIsNoMainChainNFT(string symbol, string chainId)
    {
        return symbol.Length != 0 && !CheckChainIdIsMain(chainId) &&
               CheckSymbolIsNFT(symbol);
    }

    public static bool CheckSymbolIsNFT(string symbol)
    {
        return symbol.Length != 0 &&
               Regex.IsMatch(symbol, NFTSymbolPattern);
    }

    public static bool CheckSymbolIsCommonNFTInfoId(string nftInfoId)
    {
        var parts = nftInfoId.Split(HYPHEN);
        if (parts.Length > CommonConstant.IntOne)
        {
            if (parts[CommonConstant.IntOne].Equals(SEED))
            {
                return false;
            }

            var symbol = string.Join(HYPHEN, parts, CommonConstant.IntOne, parts.Length - CommonConstant.IntOne);
            return symbol.Length != CommonConstant.IntZero &&
                   Regex.IsMatch(symbol, NFTSymbolPattern);
        }

        return false;
    }

    public static bool CheckSymbolIsNFTInfoId(string nftInfoId)
    {
        var parts = nftInfoId.Split(HYPHEN);
        if (parts.Length > CommonConstant.IntOne)
        {
            var symbol = string.Join(HYPHEN, parts, CommonConstant.IntOne, parts.Length - CommonConstant.IntOne);
            return symbol.Length != CommonConstant.IntZero &&
                   Regex.IsMatch(symbol, NFTSymbolPattern);
        }

        return false;
    }
}