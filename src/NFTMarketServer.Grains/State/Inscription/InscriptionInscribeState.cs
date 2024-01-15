namespace NFTMarketServer.Grains.State.Inscription;

public class InscriptionInscribeState
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public string TransactionId { get; set; }
    public string Tick { get; set; }
    public string Status { get; set; }
    public long Amount { get; set; }
}