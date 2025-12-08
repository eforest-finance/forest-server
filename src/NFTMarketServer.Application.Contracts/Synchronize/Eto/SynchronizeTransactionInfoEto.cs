using System;
using Volo.Abp.EventBus;

namespace NFTMarketServer.Synchronize.Eto;

[EventName("SynchronizeTransactionInfoEto")]
public class SynchronizeTransactionInfoEto
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string Symbol { get; set; }
    public string Seed { get; set; }
    public string TxHash { get; set; }
    public string ValidateTokenTxId { get; set; }
    public string ValidateTokenTx { get; set; }
    public string CrossChainCreateTokenTxId { get; set; }
    public string CrossChainTransferTxId { get; set; }
    public string CrossChainTransferTx { get; set; }
    public string SeedCrossChainReceivedTxId { get; set; }
    public long CrossChainTransferHeight { get; set; }
    public string SeedApprovedTxId { get; set; }
    public string CreateAuctionTxId { get; set; }
    public long ValidateTokenHeight { get; set; }

    // Owner agent
    public string ValidateOwnerAgentTxId { get; set; }
    public string ValidateOwnerAgentTx { get; set; }
    public string CrossChainSyncOwnerAgentTxId { get; set; }
    public long ValidateOwnerAgentHeight { get; set; }

    // Issuer agent
    public string ValidateIssuerAgentTxId { get; set; }
    public string ValidateIssuerAgentTx { get; set; }
    public string CrossChainSyncIssuerAgentTxId { get; set; }
    public long ValidateIssuerAgentHeight { get; set; }

    public string LastModifyTime { get; set; }
    public string Message { get; set; }
    public string Status { get; set; }
}