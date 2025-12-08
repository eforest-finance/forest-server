using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using MassTransit.Futures.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.HandleException;
using NFTMarketServer.Options;
using Volo.Abp;
using Volo.Abp.Caching;

namespace NFTMarketServer.Tokens
{
    [RemoteService(IsEnabled = false)]
    public class TokenAppService : NFTMarketServerAppService, ITokenAppService
    {
        private readonly ITokenMarketDataProvider _tokenMarketDataProvider;
        private readonly IDistributedCache<TokenMarketData> _distributedCache;
        private readonly ILogger<TokenAppService> _logger;
        private readonly IOptionsMonitor<TokenPriceCacheOptions> _tokenPriceCacheOptionsMonitor;

        public TokenAppService(ILogger<TokenAppService> logger, ITokenMarketDataProvider tokenMarketDataProvider,
            IDistributedCache<TokenMarketData> distributedCache, 
            IOptionsMonitor<TokenPriceCacheOptions> tokenPriceCacheOptionsMonitor)
        {
            _tokenMarketDataProvider = tokenMarketDataProvider;
            _distributedCache = distributedCache;
            _tokenPriceCacheOptionsMonitor = tokenPriceCacheOptionsMonitor;
            _logger = logger;
        }

        [ExceptionHandler(typeof(Exception),
            Message = "TokenAppService.GetHistoryPriceAsync is fail",
            TargetType = typeof(ExceptionHandlingService),
            MethodName = nameof(ExceptionHandlingService.HandleExceptionSymbolPriceRetrun),
            LogTargets = new[] { "symbol", "dateTime" }
        )]
        public virtual async Task<decimal> GetHistoryPriceAsync(string symbol, DateTime priceTime)
        {
            var price = await _tokenMarketDataProvider.GetHistoryPriceAsync(symbol, priceTime);
            return price;
        }

        public async Task<TokenMarketDataDto> GetTokenMarketDataAsync(string symbol, DateTime? time)
        {
            if (!time.HasValue)
            {
                return await GetCurrentPriceAsync(symbol);
            }

            var priceTime = time.Value.Date;
            var cacheKey = GetHistoryTokenMarketDataCacheKey(symbol, priceTime);
            var cache = await _distributedCache.GetAsync(cacheKey);
            if (cache != null)
            {
                return ObjectMapper.Map<TokenMarketData, TokenMarketDataDto>(cache);
            }

            var price = new decimal(0);
            price = await GetHistoryPriceAsync(symbol, priceTime);
            if (price <= 0)
            {
                _logger.LogInformation("GetHistoryPriceAsync e ,then defaultCache");
                var defaultCacheKey = GetDefaultTokenMarketDataCacheKey(symbol);
                var defaultMarketData = await _distributedCache.GetAsync(defaultCacheKey);
                _logger.LogInformation("GetHistoryPriceAsync e ,then defaultCache, price={A} symbol={B}",
                    defaultMarketData?.Price, defaultMarketData?.Symbol);
                return ObjectMapper.Map<TokenMarketData, TokenMarketDataDto>(defaultMarketData);
            }

            var marketData = new TokenMarketData
            {
                Price = price,
                Timestamp = priceTime,
                Symbol = symbol
            };


            await _distributedCache.SetAsync(cacheKey, marketData);
            return ObjectMapper.Map<TokenMarketData, TokenMarketDataDto>(marketData);
        }

        [ExceptionHandler(typeof(Exception),
            Message = "TokenAppService.GetCurrentPriceBySymbolNoFromCatchAsync is fail", 
            TargetType = typeof(ExceptionHandlingService), 
            MethodName = nameof(ExceptionHandlingService.HandleExceptionSymbolPriceRetrun),
            LogTargets = new []{"symbol"}
        )]
        public virtual Task<decimal> GetCurrentPriceBySymbolNoFromCatchAsync(string symbol)
        {
            var price = _tokenMarketDataProvider.GetPriceAsync(symbol);
            return price;
        }

        private async Task<TokenMarketDataDto> GetCurrentPriceAsync(string symbol)
        {
            var cacheKey = GetCurrentTokenMarketDataCacheKey(symbol);
            var cache = await _distributedCache.GetAsync(cacheKey);
            if (cache != null)
            {
                return ObjectMapper.Map<TokenMarketData, TokenMarketDataDto>(cache);
            }

            var defaultCacheKey = GetDefaultTokenMarketDataCacheKey(symbol);
            
            var price = new decimal(0);
            price = await GetCurrentPriceBySymbolNoFromCatchAsync(symbol);
            if (price <= 0)
            {
                _logger.LogInformation("GetCurrentPriceAsync e ,then defaultCache");
                var defaultMarketData = await _distributedCache.GetAsync(defaultCacheKey);
                _logger.LogInformation("GetCurrentPriceAsync e ,then defaultCache, price={A} symbol={B}",
                    defaultMarketData?.Price, defaultMarketData?.Symbol);
                return ObjectMapper.Map<TokenMarketData, TokenMarketDataDto>(defaultMarketData); 
            }
            
            var marketData = new TokenMarketData
            {
                Price = price,
                Timestamp = DateTime.UtcNow,
                Symbol = symbol
            };
            await _distributedCache.SetAsync(cacheKey, marketData, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = marketData.Timestamp.AddMinutes(_tokenPriceCacheOptionsMonitor.CurrentValue.Minutes)
            });
            
            await _distributedCache.SetAsync(defaultCacheKey, marketData, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = marketData.Timestamp.AddDays(36500)
            });

            return ObjectMapper.Map<TokenMarketData, TokenMarketDataDto>(marketData);
        }

        private string GetCurrentTokenMarketDataCacheKey(string symbol)
        {
            return $"current-{symbol}";
        }
        
        private string GetDefaultTokenMarketDataCacheKey(string symbol)
        {
            return $"default-{symbol}";
        }

        private string GetHistoryTokenMarketDataCacheKey(string symbol, DateTime time)
        {
            return $"history-{symbol}-{time}";
        }

        public async Task<decimal> GetCurrentDollarPriceAsync(string symbol, decimal symbolAmount)
        {
            var marketData = await GetCurrentPriceAsync(symbol);
            if (marketData == null)
            {
                _logger.LogError("GetCurrentDollarPriceAsync query {symbol} fail:result is null", symbol);
                return symbolAmount;
            }

            if (marketData.Price <= 0)
            {
                _logger.LogError("GetCurrentDollarPriceAsync query {symbol} fail:result<0", symbol);
                return symbolAmount;
            }

            return symbolAmount * marketData.Price;
        }
    }
}