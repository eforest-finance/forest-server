using AElf;
using NFTMarketServer.NFT;

namespace NFTMarketServer.Common;

public static class ChainIdHelper
{
    public const string UNDERLINE = "_";
    public static string MaskChainId(long chainId)
    {
        if (chainId == 0)
        {
            return "";
        }

        var chain = ChainHelper.ConvertChainIdToBase58((int)chainId);

        if (chain.Equals(SymbolHelper.MAIN_CHAIN_SYMBOL))
        {
            return SymbolHelper.MAIN_CHAIN_PREFIX + chain;
        }

        return SymbolHelper.SIDE_CHAIN_PREFIX + chain;
    }
}