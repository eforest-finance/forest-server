namespace NFTMarketServer.Basic;

public static class CommonConstant
{
    public const string Coin_ELF = "ELF";
    public const long LongError = -1;
    public const long LongEmpty = 0;
    public const int IntError = -1;
    public const string MainChainId = "AELF";
    
    public const char SymbolSeparator = '-';
    public const string CollectionSymbolSuffix = "0";
    
    public const int CacheExpirationDays = 365;

    public const string CollectionTypeSeed = "seed";
    public const string CollectionTypeNFT = "nft";
    public const string FILE_TYPE_IMAGE = "image";
    public const string TokenExist = "Token already exists";
    public const string InscriptionIssueRepeat = "Total supply exceeded";
    public const string ResetNFTSyncHeightFlagCacheKey = "ResetNFTSyncHeightFlagCacheKey";
    public const string NFTResetHeightFlagCacheKey = "NFTResetHeightFlagCacheKey";
    public const string SeedResetHeightFlagCacheKey = "SeedResetHeightFlagCacheKey";
    public const int CacheExpirationMinutes = 60*24;
    public const int BeginHeight = 1;

}