namespace NFTMarketServer.Grains.Grain.ApplicationHandler;

public class ChainOptions
{
    public Dictionary<string, ChainInfo> ChainInfos { get; set; }
}

public class SynchronizeTransactionJobOptions
{
    public int CrossChainDelay { get; set; } = 1000;
    public long RetryTimes { get; set; } = 5;
    public long BeginHeight { get; set; } = -1;
}

public class SynchronizeSeedJobOptions
{
    public string ToChainId { get; set; } 
}

public class HideCollectionInfoOptions
{
    public List<string> HideCollectionInfoList { get; set; }
}

public class CollectionTradeInfoOptions
{
    public bool GrayIsOn { get; set; } = false;
    public List<string> CollectionIdList { get; set; } = new List<string>();
}

public class ResetNFTSyncHeightExpireMinutesOptions
{
    public int ResetNFTSyncHeightExpireMinutes { get; set; }
}

public class ChoiceNFTInfoNewFlagOptions
{
    public bool ChoiceNFTInfoNewFlagIsOn { get; set; }
}

public class CollectionActivityNFTLimitOptions
{
    public int CollectionActivityNFTLimit { get; set; } = 1000;
}