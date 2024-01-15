using System;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.Symbol;

public class OwnerShipVerifyOrder : NFTMarketEntity<Guid>
{
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string IssueChain { get; set; }
    [Keyword] public string ProjectCreatorAddress { get; set; }
    [Keyword] public string IssueContractAddress { get; set; }
    [Keyword] public string Message { get; set; }
    [Keyword] public string Signature { get; set; }
    [Keyword] public string Proof { get; set; }
    [Keyword] public string From { get; set; }
    // AELF Chain
    [Keyword] public string ChainId { get; set; }
    // AELF Address
    [Keyword] public string IssueAddress { get; set; }
    public long SubmitTime { get; set; }
    [Keyword] public string OpAddress { get; set; }
    public long ApprovalTime { get; set; }
    [Keyword]  public string Comment { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public bool VerifyResult { get; set; }
    public ProposalStatus ProposalStatus { get; set; }
    [Keyword] public string ProposalTransactionId { get; set; }
}