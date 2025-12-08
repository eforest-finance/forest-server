using System;
using System.Collections.Generic;
using System.Linq;
using AElf;
using NFTMarketServer.Basic;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.Common;

public static class NFTHelper
{
    public static string GetCollectionIdPre(string collectionId)
    {
        return collectionId.Replace(CommonConstant.NftSubfix, "");
    }
    
    public static SymbolType GetCreateInputSymbolType(string symbol)
    {
        var words = symbol.Split(NFTSymbolBasicConstants.NFTSymbolSeparator);
        if (!(words[0].Length > 0 && words[0].All(IsValidCreateSymbolChar))) return SymbolType.Unknown;
        if (words.Length == 1) return SymbolType.Token;
        if (words.Length != 2 || words[1].Length <= 0 || !words[1].All(IsValidItemIdChar)) return SymbolType.Unknown;
        return words[1] == NFTSymbolBasicConstants.CollectionSymbolSuffix ? SymbolType.NftCollection : SymbolType.Nft;
    }

    private static bool IsValidCreateSymbolChar(char character)
    {
        return character >= 'A' && character <= 'Z';
    }

    private static bool IsValidItemIdChar(char character)
    {
        return character >= '0' && character <= '9';
    }

    public static string GetTick(string symbol)
    {
        var words = symbol.Split(NFTSymbolBasicConstants.NFTSymbolSeparator);
        return words.Length == 1 ? symbol : words[0];
         
    }

    public static string ConvertCollectionSymbol(string tick)
    {
        return string.Join(NFTSymbolBasicConstants.NFTSymbolSeparator, tick,
            NFTSymbolBasicConstants.CollectionSymbolSuffix);
    }

    public static string ConvertItemSymbol(string tick)
    {
        return string.Join(NFTSymbolBasicConstants.NFTSymbolSeparator, tick,
            NFTSymbolBasicConstants.FirstItemSymbolSuffix);
    }
    
    public static string GetNftImageUrl(List<IndexerExternalInfoDictionary> externalInfo, Func<string> getImageUrlFunc)
    {
        var nftImageUrl = externalInfo.FirstOrDefault(o => o.Key == "__nft_image_url")?.Value;
        if (nftImageUrl.IsNullOrEmpty())
        {
            nftImageUrl = externalInfo.FirstOrDefault(o => o.Key == "inscription_image")?.Value;
        }
        return nftImageUrl.IsNullOrEmpty() ? getImageUrlFunc() : nftImageUrl;
    }

    public static bool GetIsMainChainCreateNFT(IEnumerable<IndexerExternalInfoDictionary> externalInfo)
    {
        var nftCreateChainId = externalInfo.FirstOrDefault(o => o.Key == CommonConstant.MetadataNFTCreateChainIdKey)
            ?.Value;

        return nftCreateChainId.IsNullOrEmpty() || ChainHelper.ConvertBase58ToChainId(CommonConstant.MainChainId)
            .ToString().Equals(nftCreateChainId);
    }
}