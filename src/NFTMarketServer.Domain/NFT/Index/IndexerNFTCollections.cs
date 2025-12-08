using System;
using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerNFTCollections : IndexerCommonResult<IndexerNFTCollections>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerNFTCollection> IndexerNftCollections { get; set; }
}

public class IndexerNFTCollection : IndexerCommonResult<IndexerNFTCollection>
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public bool IsBurnable { get; set; }
    public int IssueChainId { get; set; }
    public string Creator { get; set; }
    public string ProxyOwnerAddress { get; set; }
    public string ProxyIssuerAddress { get; set; }
    public string CreatorAddress { get; set; }
    public string LogoImage { get; set; }
    public string FeaturedImage { get; set; }
    public string Description { get; set; }
    public bool IsOfficial { get; set; }
    public string BaseUrl { get; set; }
    public List<IndexerExternalInfoDictionary> ExternalInfoDictionary { get; set; }
    public long BlockHeight { get; set; }
    public DateTime CreateTime { get; set; }
}
public class IndexerExternalInfoDictionary : IndexerCommonResult<IndexerExternalInfoDictionary>
{
    public string Key { get; set; }
    public string Value { get; set; }
}