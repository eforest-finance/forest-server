using System.Collections.Generic;
using NFTMarketServer.Seed.Dto;

namespace NFTMarketServer.Bid.Dtos;

public class SeedPriceDto
{
    public  string Id { get; set; }
    
    public string TokenType { get; set; }
    
    public int SymbolLength { get; set; }
    
    public TokenPriceDto TokenPrice { get; set; }
    
    public long BlockHeight { get; set; }

}

public class SeedPriceRecordResultDto
{
    public List<SeedPriceDto> GetSeedPriceInfos { get; set; }
}