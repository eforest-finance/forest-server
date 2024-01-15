using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace NFTMarketServer.NFT.Index;

public class SynchronizeTransactionInfoIndex : NFTMarketServerEsEntity<string>, IIndexBuild
{
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string FromChainId { get; set; }
    [Keyword] public string ToChainId { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string Seed { get; set; }

    // token
    [Keyword] public string TxHash { get; set; }
    [Keyword] public string ValidateTokenTxId { get; set; }
    public string ValidateTokenTx { get; set; }
    public long ValidateTokenHeight { get; set; }
    [Keyword] public string CrossChainCreateTokenTxId { get; set; }
    [Keyword] public string CrossChainTransferTxId { get; set; }
    [Keyword] public string CrossChainTransferTx { get; set; }
    [Keyword] public long CrossChainTransferHeight { get; set; }
    [Keyword] public string SeedCrossChainReceivedTxId { get; set; }

    // Owner agent
    [Keyword] public string ValidateOwnerAgentTxId { get; set; }
    [Keyword] public string ValidateOwnerAgentTx { get; set; }
    [Keyword] public string CrossChainSyncOwnerAgentTxId { get; set; }

    public long ValidateOwnerAgentHeight { get; set; }

    // Issuer agent
    [Keyword] public string ValidateIssuerAgentTxId { get; set; }
    [Keyword] public string ValidateIssuerAgentTx { get; set; }
    [Keyword] public string CrossChainSyncIssuerAgentTxId { get; set; }
    public long ValidateIssuerAgentHeight { get; set; }

    // Auction
    [Keyword] public string SeedIssuedTxId { get; set; }
    [Keyword] public string SeedApprovedTxId { get; set; }
    [Keyword] public string CreateAuctionTxId { get; set; }

    public string Message { get; set; }
    [Keyword] public string Status { get; set; }
    [Keyword] public object LastModifyTime { get; set; }
}