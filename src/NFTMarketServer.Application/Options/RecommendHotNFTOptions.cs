using System.Collections.Generic;

namespace NFTMarketServer.Options;

public class RecommendHotNFTOptions
{
    public bool HotNFTCacheIsOn { get; set; } = false;
    public int HotNFTCacheMinutes { get; set; } = 1;
    public List<RecommendHotNFT> RecommendHotNFTList { get; set; }
}

public class RecommendHotNFT
{
    public string NFTInfoId { get; set; }
    public string Link { get; set; }
}