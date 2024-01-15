using System;
using System.Threading.Tasks;
using CoinGecko.Clients;
using CoinGecko.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NFTMarketServer.Tokens;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.CoinGeckoApi
{
    public class TokenMarketDataProvider : ITokenMarketDataProvider, ITransientDependency
    {
        private readonly ICoinGeckoClient _coinGeckoClient;
        private readonly IRequestLimitProvider _requestLimitProvider;
        private readonly IOptionsMonitor<CoinGeckoOptions> _coinGeckoOptionsMonitor;

        private const string UsdSymbol = "usd";

        public ILogger<TokenMarketDataProvider> Logger { get; set; }

        public TokenMarketDataProvider(IRequestLimitProvider requestLimitProvider, IOptionsMonitor<CoinGeckoOptions> coinGeckoOptionsMonitor)
        {
            _requestLimitProvider = requestLimitProvider;
            _coinGeckoClient = CoinGeckoClient.Instance;
            _coinGeckoOptionsMonitor = coinGeckoOptionsMonitor;

            Logger = NullLogger<TokenMarketDataProvider>.Instance;
        }
        
        public async Task<decimal> GetPriceAsync(string symbol)
        {
            var coinId = GetCoinIdAsync(symbol);
            if (coinId == null)
            {
                Logger.LogWarning($"can not get the token {symbol}");
                return 0;
            }

            try
            {
                var coinData =
                    await RequestAsync(async () =>
                        await _coinGeckoClient.SimpleClient.GetSimplePrice(new[] {coinId}, new[] { UsdSymbol }));

                if (!coinData.TryGetValue(coinId,out var value))
                {
                    return 0;
                }

                return value[UsdSymbol].Value;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Can not get current price from CoinGecko service for " + symbol);
                throw new Exception("Can not get current price from CoinGecko service for " + symbol);
            }
        }

        public async Task<decimal> GetHistoryPriceAsync(string symbol, DateTime dateTime)
        {
            var coinId = GetCoinIdAsync(symbol);
            if (coinId == null)
            {
                Logger.LogWarning($"can not get the token {symbol}");
                return 0;
            }

            try
            {
                // var proxy = new WebProxy
                // {
                //     Address = new Uri("http://127.0.0.1:1087"),
                // };
                // var clientHandler = new HttpClientHandler()
                // {
                //     Proxy = proxy,
                // };
                // var client = new CoinGeckoClient(clientHandler);
                
                var coinData =
                    await RequestAsync(async () => await _coinGeckoClient.CoinsClient.GetHistoryByCoinId(coinId,
                        dateTime.ToString("dd-MM-yyyy"), "false"));

                if (coinData.MarketData == null)
                {
                    return 0;
                }

                return (decimal) coinData.MarketData.CurrentPrice[UsdSymbol].Value;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"can not get :{symbol} price.");
                throw;
            }
        }

        private string GetCoinIdAsync(string symbol)
        {
            return _coinGeckoOptionsMonitor.CurrentValue.CoinIdMapping.TryGetValue(symbol.ToUpper(), out var id) ? id : null;
        }

        private async Task<T> RequestAsync<T>(Func<Task<T>> task)
        {
            await _requestLimitProvider.RecordRequestAsync();
            return await task();
        }
    }
}