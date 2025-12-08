using System;
using NFTMarketServer.Entities;

namespace NFTMarketServer.Tokens
{
    public class TokenMarketData : NFTMarketEntity<Guid>
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}