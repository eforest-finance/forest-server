namespace NFTMarketServer.Grains.Grain.Icon;
[GenerateSerializer]
public class SymbolIconGrainDto
{
    [Id(0)]
    public string Symbol { get; set; }
    [Id(1)]
    public string Icon { get; set; }
}