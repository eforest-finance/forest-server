namespace NFTMarketServer.Grains.Grain.Synchronize;

public static class SynchronizeTransactionJobStatus
{
    public const string TokenCreating = "TokenCreating";
    public const string TokenValidating = "TokenValidating";
    public const string CrossChainTokenCreating = "CrossChainTokenCreating";
    public const string CrossChainTokenCreated = "CrossChainTokenCreated";
    public const string WaitingIndexing = "WaitingIndexing";

    // ProxyAccountsAndToken
    public const string ProxyAccountsAndTokenCreating = "ProxyAccountsAndTokenCreating";
    public const string ProxyAccountsAndTokenValidating = "ProxyAccountsAndTokenValidating";
    public const string WaitingProxyAccountsAndTokenIndexing = "WaitingProxyAccountsAndTokenIndexing";
    public const string CrossChainProxyAccountsAndTokenSyncing = "CrossChainProxyAccountsAndTokenSyncing";

    // Seed
    public const string SeedCreating = "SeedCreating";
    public const string SeedCreated = "SeedCreated";
    public const string SeedValidating = "SeedValidating";
    public const string SeedWaitingIndexing = "SeedWaitingIndexing";
    public const string SeedApproving = "SeedApproving";
    public const string SeedCrossChainTransferIndexing = "SeedCrossChainTransferIndexing";
    public const string SeedCrossChainReceiving = "SeedCrossChainReceiving";
    public const string SeedCrossChainCreating = "SeedCrossChainCreating";
    public const string SeedCrossChainTransferring = "SeedCrossChainTransferring";
    public const string SeedCreateAuction = "SeedCreateAuction";

    // auction
    public const string AuctionCreating = "AuctionCreating";
    public const string AuctionCreated = "AuctionCreated";

    public const string Failed = "Failed";
}

public static class TokenStatus
{
    public const string AlreadyExists = "AlreadyExists";
    public const string Unknown = "Unknown";
    public const string NotExist = "NotExist";
    public const string UnknownChainId = "UnknownChainId";
}