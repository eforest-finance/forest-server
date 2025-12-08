namespace NFTMarketServer.Dealer.Dtos;

public enum ContractInvokeSendStatus
{
    NotSend = 0,
    Sent = 1,
    Success = 3,
    Failed = 4,
}

public enum TransactionResultStatus
{
    // not existed
    NOTEXISTED = 0,
    // pending 
    PENDING = 1,
    // failed
    FAILED = 2,
    // mined
    MINED = 3,
    // conflict
    CONFLICT = 4,
    // pending validation
    PENDINGVALIDATION = 5,
    // node validation failed
    NODEVALIDATIONFAILED = 6,
}

public enum BizType
{
    CreateSeed = 0,
    AuctionClaim = 1,
    InscriptionCollectionValidateTokenInfoExists = 2,
    InscriptionItemValidateTokenInfoExists = 3,
    InscriptionCollectionCrossChainCreate = 4,
    InscriptionItemCrossChainCreate = 5,
    InscriptionIssue = 6,
    NFTDropFinish = 7,
}
