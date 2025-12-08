using System;
using System.Threading.Tasks;

namespace NFTMarketServer.Tokens
{
    public interface ITokenAppService
    {
        Task<TokenMarketDataDto> GetTokenMarketDataAsync(string symbol, DateTime? time);

        Task<decimal> GetCurrentDollarPriceAsync(string symbol, decimal amount);
    }
}