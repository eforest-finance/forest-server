using System.Collections.Generic;

namespace NFTMarketServer.NFT;

public class GetCollectionActivityListInput
{
    public string CollectionId { get; set; }
    public List<string> BizIdList { get; set; }
    public List<int> Types { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}