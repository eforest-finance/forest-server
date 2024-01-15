namespace SymbolMarketServer.Options;


public class CacheOptions
{
    public int ExpirationDays { get; set; }
    public int CoinGeckoExpirationMinutes { get; set; } = 5;
}