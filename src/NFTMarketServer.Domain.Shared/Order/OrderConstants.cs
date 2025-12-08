namespace NFTMarketServer.Order;

public enum OrderStatus
{
    Init = 0,
    UnPay = 1,
    Payed = 2,
    Notifying = 3,
    NotifySuccess = 4,
    NotifyFail = 5,
    Finished = 6,
    Failed = 7,
    Expired = 8,
    Cancelled = 9
}

public enum PortkeyOrderStatus
{
    Pending,
    Failed,
    Expired
}

public class OrderConstants
{
    public const long OrderLockSeedExpireTime = 30 * 60;
    public const string LocalMerchantName = "SymbolMarket";
    public const string Success = "SUCCESS";
    public const string Fail = "FAIL";
    public const string DefaultNetWork = "ELF";
    public const string DefaultChainId = "AELF";
    public const string NftBuy = "NFTBuy";
}