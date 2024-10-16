namespace NFTMarketServer.Grains.Grain.Inscription;
[GenerateSerializer]
public class InscriptionItemCrossChainGrainDto
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string ValidateRawTransaction { get; set; }
    [Id(2)]
    public string FromChainId { get; set; }
    [Id(3)]
    public string ToChainId { get; set; }
    [Id(4)]
    public string Symbol { get; set; }
    [Id(5)]
    public long ParentChainHeight { get; set; }
    [Id(6)]
    public string TransactionId { get; set; }
    [Id(7)]
    public bool IsCollectionCreated { get; set; }
}