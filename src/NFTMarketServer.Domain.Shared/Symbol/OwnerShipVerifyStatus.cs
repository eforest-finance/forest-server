namespace NFTMarketServer.Symbol;

public enum ApprovalStatus
{
    Unknown = 0,
    Pending = 1,
    AutoApproved = 2,
    Approved = 3,
    Rejected = 4
}

public enum ProposalStatus
{
    Unknown = 0,
    Unsent = 1,
    Sent = 2,
    Success = 3,
    Fail = 4
}