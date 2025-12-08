namespace NFTMarketServer.Grains.State.Synchronize;

public class SynchronizeAITokenState
{
    public string Id { get; set; }
    public string ToChainId { get; set; }
    public string FromChainId { get; set; }
    public string Message { get; set; }
    public string Symbol { get; set; }
    public string ValidateTokenTxId { get; set; }
    public string ValidateTokenTx { get; set; }
    public string CrossChainCreateTokenTxId { get; set; }
    public string CrossChainTransferTxId { get; set; }
    public string CrossChainTransferTx { get; set; }
    public long CrossChainTransferHeight { get; set; }
    public long ValidateTokenHeight { get; set; }

    public string Status { get; set; }
    
    public long CreateTime{ get; set; }
    public long UpdateTime{ get; set; }

}