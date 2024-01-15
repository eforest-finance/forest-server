namespace NFTMarketServer.Synchronize.Dto;

public class SyncResultDto
{
    public string TxHash { get; set; }
    public string ValidateTokenTxId { get; set; }
    public string CrossChainCreateTokenTxId { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
}

public class TokenStatusDto
{
    public string Status { get; set; }
    public string Symbol { get; set; }
}

public class SendNFTSyncResponseDto
{
}