using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerUserMatchedNftIds : IndexerCommonResult<IndexerUserMatchedNftIds>
{ 
    public List<string> NftIds { get; set; }
}