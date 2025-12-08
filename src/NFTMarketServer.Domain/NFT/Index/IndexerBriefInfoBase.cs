using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerBriefInfoBase
{
    public string CollectionSymbol { get; set; }
    public string NFTSymbol { get; set; }
    public string PreviewImage { get; set; } 
    public string PriceDescription { get; set; }
    public decimal Price { get; set; }
    
    public string Id { get; set; }
    public string TokenName { get; set; }
    public string IssueChainIdStr { get; set; }
    public string ChainIdStr { get; set; }
}


public class IndexerNFTBriefInfo : IndexerBriefInfoBase
{
    
}

public class IndexerSeedBriefInfo : IndexerBriefInfoBase
{
    
}

public class IndexerNFTBriefInfos : IndexerCommonResult<IndexerNFTBriefInfos>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerNFTBriefInfo> IndexerNFTBriefInfoList { get; set; }
}

public class IndexerSeedBriefInfos : IndexerCommonResult<IndexerSeedBriefInfos>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerSeedBriefInfo> IndexerSeedBriefInfoList { get; set; }
}

public enum TokenType
{
    FT,
    NFT
}