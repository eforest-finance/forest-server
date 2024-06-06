using System.Collections.Generic;

namespace NFTMarketServer.NFT.Dtos;

public class SchrodingerSymbolIndexerListDto
{
    public long TotalCount { get; set; }
    public List<SchrodingerSymbolIndexerDto> Data { get; set; }
}
public class SchrodingerSymbolIndexerDto
{
    public string Symbol { get; set; }
    public string TokenName { get; set; }
    public string InscriptionImageUri { get; set; }
    public long Amount { get; set; }
    public int Generation { get; set; }
    public int Decimals { get; set; }
    public string InscriptionDeploy { get; set; }
    public string Adopter { get; set; }
    public long AdoptTime { get; set; }
    public string Tick { get; set; }
    public List<TraitsInfo> Traits { get; set; }
    public int Rank { get; set; }
    public string Level { get; set; }
    public string Grade { get; set; }
    public string Star { get; set; }
    public string Rarity { get; set; }
}
public class TraitsInfo
{
    public string TraitType { get; set; }
    public string Value { get; set; }
}
public class SchrodingerSymbolIndexerQuery
{
    public SchrodingerSymbolIndexerListDto GetAllSchrodingerList { get; set; }
}