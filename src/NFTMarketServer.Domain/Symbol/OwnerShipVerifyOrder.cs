using System;
using Nest;
using NFTMarketServer.Entities;
using Orleans;

namespace NFTMarketServer.Symbol;
[GenerateSerializer]
public class OwnerShipVerifyOrder : NFTMarketEntity<Guid>
{
    [Keyword][Id(0)] public string Symbol { get; set; }
    [Keyword][Id(1)] public string IssueChain { get; set; }
    [Keyword][Id(2)] public string ProjectCreatorAddress { get; set; }
    [Keyword][Id(3)] public string IssueContractAddress { get; set; }
    [Keyword][Id(4)] public string Message { get; set; }
    [Keyword][Id(5)] public string Signature { get; set; }
    [Keyword][Id(6)] public string Proof { get; set; }
    [Keyword][Id(7)] public string From { get; set; }
    // AELF Chain
    [Keyword][Id(8)] public string ChainId { get; set; }
    // AELF Address
    [Keyword][Id(9)] public string IssueAddress { get; set; }
    [Id(10)]
    public long SubmitTime { get; set; }
    [Keyword][Id(11)] public string OpAddress { get; set; }
    [Id(12)]
    public long ApprovalTime { get; set; }
    [Keyword][Id(13)] public string Comment { get; set; }
    [Id(14)]
    public ApprovalStatus ApprovalStatus { get; set; }
    [Id(15)]
    public bool VerifyResult { get; set; }
    [Id(16)]
    public ProposalStatus ProposalStatus { get; set; }
    [Keyword][Id(17)] public string ProposalTransactionId { get; set; }
}