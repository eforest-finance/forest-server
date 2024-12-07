using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace NFTMarketServer.NFT.Index;

public class SynchronizeAITokenJobInfoIndex : NFTMarketServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string FromChainId { get; set; }
    [Keyword] public string ToChainId { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string ValidateTokenTxId { get; set; }
    [Keyword] public string ValidateTokenTx { get; set; }
    public long ValidateTokenHeight { get; set; }
    [Keyword] public string CrossChainCreateTokenTxId { get; set; }
    [Keyword] public string CrossChainTransferTxId { get; set; }
    [Keyword] public string CrossChainTransferTx { get; set; }
    public long CrossChainTransferHeight { get; set; }

    public string Message { get; set; }
    [Keyword] public string Status { get; set; }
    public long LastModifyTime { get; set; }
    public long CreateTime{ get; set; }

}