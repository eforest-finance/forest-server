using System;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public class SeedSymbolIndexDto : EntityDto<string>
    {
        public string Symbol { get; set; }
        public string SeedSymbol { get; set; }
        
        public string Issuer { get; set; }

        public bool IsBurnable { get; set; }

        public DateTime CreateTime { get; set; }
        
        public long CreateTimeStamp { get; set; }

        public long SeedExpTimeSecond { get; set; }

        public DateTime SeedExpTime { get; set; }
    }
}