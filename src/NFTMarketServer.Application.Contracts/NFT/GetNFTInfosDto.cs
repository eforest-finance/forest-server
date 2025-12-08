using System.Collections.Generic;

namespace NFTMarketServer.NFT;

public class GetNFTInfosDto
{
    public List<string> CollectionIdList { get; set; }
    public List<string> ChainList { get; set; }
    public List<string> NFTIdList { get; set; }
    public List<TraitDto> Traits { get; set; }
    public int MaxLimit { get; set; }

    public string SearchParam { get; set; }
}