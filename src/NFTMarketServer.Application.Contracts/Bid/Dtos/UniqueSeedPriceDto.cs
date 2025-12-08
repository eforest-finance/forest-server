using System.Collections.Generic;
using NFTMarketServer.Seed.Dto;

namespace NFTMarketServer.Bid.Dtos;

public class UniqueSeedPriceDto
{
    public  string Id { get; set; }
    
    public string TokenType { get; set; }
    
    public int SymbolLength { get; set; }
    
    public TokenPriceDto TokenPrice { get; set; }
    public long BlockHeight { get; set; }
}

public class UniqueSeedPriceRecordResultDto
{
    public List<UniqueSeedPriceDto> GetUniqueSeedPriceInfos { get; set; }
}