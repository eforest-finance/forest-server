namespace NFTMarketServer.Synchronize.Dto;

public class SynchronizeTransactionDto
{
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string Symbol { get; set; }
    public string TxHash { get; set; }
    public string CreateTokenTx { get; set; }
    public string ValidateTokenTxId { get; set; }
    public string ValidateTokenTx { get; set; }
    public long ValidateTokenHeight { get; set; }
    public string CrossChainCreateTokenTxId { get; set; }

    public string Message { get; set; }
    public string Status { get; set; }
}