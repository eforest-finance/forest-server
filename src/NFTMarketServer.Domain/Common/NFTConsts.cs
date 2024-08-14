namespace NFTMarketServer.Basic;

public enum SymbolType
{
    Token,
    Nft,
    NftCollection,
    Unknown
}

public static class NFTSymbolBasicConstants
{
    public const char NFTSymbolSeparator = '-';
    public const string MainChain = "AELF";
    public const string SeedNamePrefix = "SEED";
    public const string CollectionSymbolSuffix = "0";
    public const string FirstItemSymbolSuffix = "1";
    public const string SeedCollectionSymbol = "SEED-0";
    public const string BrifeInfoDescriptionPrice = "Price";
    public const string BrifeInfoDescriptionOffer = "Offer";
    public const string BrifeInfoDescriptionTopBid = "Top Bid";
    public const string SeedIdPattern = @"^[a-zA-Z]{4}-SEED-[0-9]+$";
    public const string SeedZeroIdPattern = @"^[a-zA-Z]{4}-SEED-0$";
    public const int NFTInfoQueryStatusSelf = 2;
    
    public const string UserBalanceScriptForNft =
        "doc['symbol'].value =~ /.-[1-9]{1,}/ && !doc['symbol'].value.contains('SEED-')";
    public const string UserBalanceScriptForSeed = "doc['symbol'].value =~ /^SEED-[0-9]+$/";
    public const string Painless =
        "painless";
    public const string  BurnedAllNftScript = "doc['supply'].value == 0 && doc['issued'].value == doc['totalSupply'].value";
    public const string IssuedLessThenOneGetThenZeroANftScript = "((doc['supply'].value / Math.pow(10, doc['decimals'].value)) < 1) && ((doc['supply'].value / Math.pow(10, doc['decimals'].value)) > 0)";
    public const string  CreateFailedANftScript = "doc['supply'].value == 0 && doc['issued'].value == 0";
    public const char StatisticsKeySeparator = ':';

}