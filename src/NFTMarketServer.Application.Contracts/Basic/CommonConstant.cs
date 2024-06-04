namespace NFTMarketServer.Basic;

public static class CommonConstant
{
    public const string Coin_ELF = "ELF";
    public const int Coin_ELF_Decimals = 8;
    public const long LongError = -1;
    public const long LongEmpty = 0;
    public const int IntZero = 0;
    public const int IntOne = 1;
    public const int IntThree = 3;
    public const int IntNegativeOne = -1;
    public const int IntTen = 10;
    public const int RecentSearchNumber = 30000;
    public const long LongOne = 1;
    public const int IntError = -1;
    public const string MainChainId = "AELF";
    public const string NFT_ExternalInfo_Metadata_Key = "__nft_metadata";
    public const string NFT_ExternalInfo_Attributes_Key = "__nft_attributes";
    public const string NFT_ExternalInfo_InscriptionDeploy_Key = "__inscription_deploy";
    public const string NFT_ExternalInfo_Inscription_Adopt_Key = "__inscription_adopt";
    public const string MetadataSpecialInscriptionImageKey = "inscription_image";
    public const string MetadataNFTCreateChainIdKey = "__nft_create_chain_id";
    public const string MetadataImageUrlKey = "__nft_image_url";
    public const string MetadataImageUriKey = "__nft_image_uri";
    public const string MetadataImageUriKeyPre = "ipfs://";
    public const string ImageIpfsUrlPre = "https://ipfs.io/ipfs/";
    public const string ES_NFT_TraitPairsDictionary_Path = "traitPairsDictionary";
    public const string Graphql_Method = "getSyncNFTInfoRecord";
    public const string GraphqlMethodGetSyncSeedSymbolRecord = "getSyncSeedSymbolRecord";
    
    public const char SymbolSeparator = '-';
    public const string CollectionSymbolSuffix = "0";
    
    public const int CacheExpirationDays = 365;
    public const long EsLimitTotalNumber = 10000;
    public const int CollectionActivityNFTLimit = 1000;
    
    public const string CollectionTypeSeed = "seed";
    public const string CollectionTypeNFT = "nft";
    public const string FILE_TYPE_IMAGE = "image";
    public const string TokenExist = "Token already exists";
    public const string InscriptionIssueRepeat = "Total supply exceeded";
    public const string ResetNFTSyncHeightFlagCacheKey = "ResetNFTSyncHeightFlagCacheKey";
    public const string NFTResetHeightFlagCacheKey = "NFTResetHeightFlagCacheKey";
    public const string SeedResetHeightFlagCacheKey = "SeedResetHeightFlagCacheKey";
    public const string HotNFTInfosCacheKey = "HotNFTInfosCacheKey";
    
    public const string ResetNFTNewSyncHeightFlagCacheKey = "ResetNFTNewSyncHeightFlagCacheKey";
    public const string NFTNewResetHeightFlagCacheKey = "NFTNewResetHeightFlagCacheKey";
    public const string BearerToken = "Bearer ";
    public const string Authorization = "Authorization";
    public const string Underscore = "_";
    public const string ImagePNG = ".png";
    public const string MethodManagerForwardCall = "ManagerForwardCall";
    public const string MethodCreateArt = "CreateArt";
    
    public const int CacheExpirationMinutes = 60*24;
    public const int BeginHeight = 1;
    public const int HttpSuccessCode = 200;
    public const int HttpFileUploadSuccessCode = 20000;
    public const long DefaultValueNone = -1;
    public const long OneDayBlockHeight = 172800;
    public const int Gen9 = 9;


}