namespace NFTMarketServer.Grains.Grain.Synchronize;
[GenerateSerializer]
public class SynchronizeTxJobGrainDto
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string FromChainId { get; set; }
    [Id(2)]
    public string ToChainId { get; set; }
    [Id(3)]
    public string Symbol { get; set; }
    [Id(4)]
    public string Seed { get; set; }
    [Id(5)]
    public string TxHash { get; set; }
    [Id(6)]
    public string ValidateTokenTxId { get; set; }
    [Id(7)]
    public string ValidateTokenTx { get; set; }
    [Id(8)]
    public string CrossChainCreateTokenTxId { get; set; }
    [Id(9)]
    public string CrossChainTransferTxId { get; set; }
    [Id(10)]
    public string CrossChainTransferTx { get; set; }
    [Id(11)]
    public long CrossChainTransferHeight { get; set; }
    [Id(12)]
    public string SeedCrossChainReceivedTxId { get; set; }
    [Id(13)]
    public long ValidateTokenHeight { get; set; }

    // Owner agent
    [Id(14)]
    public string ValidateOwnerAgentTxId { get; set; }
    [Id(15)]
    public string ValidateOwnerAgentTx { get; set; }
    [Id(16)]
    public string CrossChainSyncOwnerAgentTxId { get; set; }
    [Id(17)]
    public long ValidateOwnerAgentHeight { get; set; }

    // Issuer agent
    [Id(18)]
    public string ValidateIssuerAgentTxId { get; set; }
    [Id(19)]
    public string ValidateIssuerAgentTx { get; set; }
    [Id(20)]
    public string CrossChainSyncIssuerAgentTxId { get; set; }
    [Id(21)]
    public long ValidateIssuerAgentHeight { get; set; }

    // Auction
    [Id(22)]
    public string SeedIssuedTxId { get; set; }
    [Id(23)]
    public string SeedApprovedTxId { get; set; }
    [Id(24)]
    public string CreateAuctionTxId { get; set; }

    [Id(25)]
    public string Message { get; set; }
    [Id(26)]
    public string Status { get; set; }
}
[GenerateSerializer]
public class CreateSynchronizeTransactionJobGrainDto
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string FromChainId { get; set; }
    [Id(2)]
    public string ToChainId { get; set; }
    [Id(3)]
    public string Symbol { get; set; }
    [Id(4)]
    public string TxHash { get; set; }
    [Id(5)]
    public string Status { get; set; }
}
[GenerateSerializer]
public class CreateSeedJobGrainDto
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string Seed { get; set; }
    [Id(2)]
    public string Status { get; set; }
}