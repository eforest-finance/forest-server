namespace NFTMarketServer.Grains.Grain.Inscription;
[GenerateSerializer]
public class InscriptionAmountGrainDto
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string Tick { get; set; }
    [Id(2)]
    public long TotalAmount { get; set; }
}