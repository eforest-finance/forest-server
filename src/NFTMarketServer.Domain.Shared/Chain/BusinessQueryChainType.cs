namespace NFTMarketServer.Chain;

public enum BusinessQueryChainType
{
    SymbolAuction,
    SymbolBid,
    TsmSeedIcon,
    CollectionExtenstion,
    CollectionExtenstionCurrentInit,
    CollectionPrice,
    TsmSeedSymbolMainChain,
    TsmSeedSymbolSideChain,
    SeedPriceRule,
    RegularPriceRule,
    UniquePriceRule,
    SeedMainChainCreateSync,
    SeedAutoClaim,
    NftInfoSync,
    NftInfoNewSync,
    NftInfoNewRecentSync,
    SeedSymbolSync,
    ExpiredNftMinPriceSync,
    ExpiredNftMaxOfferSync,
    ExpiredListingNftHandle,
    NftListingChangeNoMainChain,
    NftOfferSync,
    InscriptionCrossChain,
    NFTActivityMessageSync,
    NFTActivitySync,
    NFTActivityTransferSync,
    UserBalanceSync
}