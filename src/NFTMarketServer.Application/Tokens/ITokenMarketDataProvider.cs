using System;
using System.Threading.Tasks;

namespace NFTMarketServer.Tokens
{
    public interface ITokenMarketDataProvider
    {
        Task<decimal> GetPriceAsync(string symbol);
        Task<decimal> GetHistoryPriceAsync(string symbol, DateTime dateTime);
    }
}