namespace NFTMarketServer.Grains.Grain.Synchronize.Ai;

[GenerateSerializer]
public class SynchronizeAITokenJobGrainDto
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string ToChainId { get; set; }
    [Id(2)] public string FromChainId { get; set; }
    [Id(3)] public string Message { get; set; }
    [Id(4)] public string Symbol { get; set; }
    [Id(5)] public string ValidateTokenTxId { get; set; }
    [Id(6)] public string ValidateTokenTx { get; set; }
    [Id(7)] public string CrossChainCreateTokenTxId { get; set; }
    [Id(8)] public string CrossChainTransferTxId { get; set; }
    [Id(9)] public string CrossChainTransferTx { get; set; }
    [Id(10)] public long CrossChainTransferHeight { get; set; }
    [Id(11)] public long ValidateTokenHeight { get; set; }

    [Id(12)] public string Status { get; set; }

    [Id(13)] public long CreateTime { get; set; }
    [Id(14)] public long UpdateTime { get; set; }
}

[GenerateSerializer]
public class SaveSynchronizeAITokenJobGrainDto
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string Symbol { get; set; }
    [Id(2)] public string Status { get; set; }
    [Id(3)] public string Message { get; set; }
    [Id(4)] public string ToChainId { get; set; }
    [Id(5)] public string FromChainId { get; set; }
}

public class CrossCreateAITokenStatus
{
    public const string Failed = "Failed";
    public const string TokenCreating = "TokenCreating";
    public const string TokenValidating = "TokenValidating";
    public const string CrossChainTokenCreating = "CrossChainTokenCreating";
    public const string CrossChainTokenCreated = "CrossChainTokenCreated";
    public const string WaitingIndexing = "WaitingIndexing";
}