using System.Collections.Generic;

namespace NFTMarketServer.Common.AElfSdk.Dtos;

public class AelfScanTokenAppResponse
{
    public CurrentVersion CurrentVersion { get; set; }
}

public class CurrentVersion
{
    public List<CurrentVersionItem> Items { get; set; }
}

public class CurrentVersionItem
{
    public long LastIrreversibleBlockHeight { get; set; }
    public string ChainId { get; set; }
}