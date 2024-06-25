using System.Collections.Generic;

namespace NFTMarketServer.NFT;

public class SearchCollectionsFloorPriceInput
{
    public string ChainId { get; set; }
    public List<string> CollectionSymbolList { get; set; }
    
    public List<string> CollectionIdList { get; set; }

}