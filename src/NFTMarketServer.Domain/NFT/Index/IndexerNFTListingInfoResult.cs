using System;
using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTListingInfoResult
{
    public string ChainId { get; set; }
    
    public string Symbol { get; set; }
    
    public string NftInfoId { get; set; }

    public string CollectionSymbol { get; set; }
    
    public decimal Prices { get; set; }
    
    public DateTime ExpireTime { get; set; }
}

public class IndexerNFTListingInfoResultDto
{
    public List<IndexerNFTListingInfoResult> GetExpiredListingNft { get; set; }
}