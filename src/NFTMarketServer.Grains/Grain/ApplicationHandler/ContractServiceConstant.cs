namespace NFTMarketServer.Grains.Grain.ApplicationHandler;

public static class MethodName
{
    public const string GetParentChainHeight = "GetParentChainHeight";
    public const string CrossChainCreateToken = "CrossChainCreateToken";
    public const string CrossChainTransfer = "CrossChainTransfer";
    public const string ValidateTokenInfoExists = "ValidateTokenInfoExists";
    public const string GetTokenInfo = "GetTokenInfo";
    public const string CrossChainSyncProxyAccount = "CrossChainSyncProxyAccount";
    public const string GetProxyAccountByHash = "GetProxyAccountByHash";
    public const string ValidateProxyAccountExists = "ValidateProxyAccountExists";

    // seed and auction
    public const string CreateSeed = "CreateSeed";
    public const string Issue = "Issue";
    public const string Approve = "Approve";
    public const string CreateAuction = "CreateAuction";
    public const string GetSpecialSeed = "GetSpecialSeed";
    public const string GetAuctionConfig = "GetAuctionConfig";
    public const string IssueSeed = "IssueSeed";
    public const string CrossChainReceiveToken = "CrossChainReceiveToken";
}

public static class TransactionState
{
    public const string Mined = "MINED";
    public const string Pending = "PENDING";
    public const string Notexisted = "NOTEXISTED";
}