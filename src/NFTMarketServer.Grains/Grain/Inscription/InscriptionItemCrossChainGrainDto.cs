namespace NFTMarketServer.Grains.Grain.Inscription;

public class InscriptionItemCrossChainGrainDto
{
    public string Id { get; set; }
    public string ValidateRawTransaction { get; set; }
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string Symbol { get; set; }
    public long ParentChainHeight { get; set; }
    public string TransactionId { get; set; }
    public bool IsCollectionCreated { get; set; }
}