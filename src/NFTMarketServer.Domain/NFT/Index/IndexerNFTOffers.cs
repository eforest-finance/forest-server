using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTOffers : IndexerCommonResult<IndexerNFTOffers>
{
    public long TotalRecordCount { get; set; }
    [CanBeNull] public List<IndexerNFTOffer> IndexerNFTOfferList { get; set; }
}

public class IndexerNFTOffer : IndexerCommonResult<IndexerNFTOffer>
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public decimal Price { get; set; }
    public long Quantity { get; set; }
    public DateTime ExpireTime { get; set; }
    public string BizInfoId { get; set; }
    
    public string BizSymbol { get; set; }
    public IndexerNFTOfferPurchaseToken PurchaseToken { get; set; }
    public long RealQuantity { get; set; }

    
}