namespace NFTMarketServer.Grains.Grain.Inscription;
[GenerateSerializer]
public class InscriptionInscribeGrainDto
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public string TransactionId { get; set; }
    [Id(3)]
    public string Tick { get; set; }
    [Id(4)]
    public string Status { get; set; }
    [Id(5)]
    public long Amount { get; set; }
}