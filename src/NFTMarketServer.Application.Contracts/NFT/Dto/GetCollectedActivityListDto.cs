using System.Collections.Generic;

namespace NFTMarketServer.NFT.Dto;

public class GetCollectedActivityListDto
{
    public List<string> CollectionIdList { get; set; }
    public List<string> ChainList { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public List<string> NFTInfoIds { get; set; }
    public List<NFTActivityType> TypeList { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}