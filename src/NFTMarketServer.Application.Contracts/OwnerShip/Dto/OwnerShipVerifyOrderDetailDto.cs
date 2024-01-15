using System;
using NFTMarketServer.Symbol;

namespace NFTMarketServer.OwnerShip.Dto;

public class OwnerShipVerifyOrderDetailDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string IssueChain { get; set; }
    public string ProjectCreatorAddress { get; set; }
    public string Message { get; set; }
    public string Signature { get; set; }
    public string Proof { get; set; }
    public string From { get; set; }
    public long SubmitTime { get; set; }
    public long ApprovalTime { get; set; }
    public string Comment { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public bool VerifyResult { get; set; }
}