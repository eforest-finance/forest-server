namespace NFTMarketServer.Basic;

public static class CommonConstant
{
    public const string Coin_ELF = "ELF";
    public const long LongError = -1;
    public const long LongEmpty = 0;
    public const int IntZero = 0;
    public const int IntOne = 1;
    public const int IntTen = 10;
    public const long LongOne = 1;
    public const int IntError = -1;
    public const string MainChainId = "AELF";
    public const string NFT_ExternalInfo_Metadata_Key = "__nft_metadata";
    public const string MetadataInscriptionImageKey = "inscription_image";
    public const string ES_NFT_TraitPairsDictionary_Path = "traitPairsDictionary";
    public const string Graphql_Method = "getSyncNFTInfoRecord";
    
    public const char SymbolSeparator = '-';
    public const string CollectionSymbolSuffix = "0";
    
    public const int CacheExpirationDays = 365;
    public const long EsLimitTotalNumber = 10000;
    
    public const string CollectionTypeSeed = "seed";
    public const string CollectionTypeNFT = "nft";
    public const string FILE_TYPE_IMAGE = "image";
    public const string TokenExist = "Token already exists";
    public const string InscriptionIssueRepeat = "Total supply exceeded";
    public const string ResetNFTSyncHeightFlagCacheKey = "ResetNFTSyncHeightFlagCacheKey";
    public const string NFTResetHeightFlagCacheKey = "NFTResetHeightFlagCacheKey";
    public const string SeedResetHeightFlagCacheKey = "SeedResetHeightFlagCacheKey";
    
    public const string ResetNFTNewSyncHeightFlagCacheKey = "ResetNFTNewSyncHeightFlagCacheKey";
    public const string NFTNewResetHeightFlagCacheKey = "NFTNewResetHeightFlagCacheKey";
    
    public const int CacheExpirationMinutes = 60*24;
    public const int BeginHeight = 1;
    public const int HttpSuccessCode = 200;
    public const int HttpFileUploadSuccessCode = 20000;
    public const long DefaultValueNone = -1;
    
    

}