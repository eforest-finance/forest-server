using System.Collections.Generic;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.Seed.Dto;

public class MySeedDto
{
    public long TotalRecordCount { get; set; }
    public List<SeedDto> SeedDtoList { get; set; }
}

public class SeedDto : IndexerCommonResult<SeedDto>
{
    public string Id { get; set; }
    public string Symbol { get; set; }
    public string SeedSymbol { get; set; }
    public string SeedName { get; set; }
    public string SeedImage { get; set; }
    public SeedStatus Status { get; set; }
    public long RegisterTime { get; set; }
    public long ExpireTime { get; set; }
    public string TokenType { get; set; }
    public SeedType SeedType { get; set; }
    public AuctionType AuctionType { get; set; }
    public string Owner { get; set; }
    public string Creator { get; set; }
    public string ChainId { get; set; }
    public TokenPriceDto TokenPrice { get; set; }
    public TokenPriceDto UsdPrice { get; set; }
    public long CreateTime { get; set; }
    
    public long BlockHeight  { get; set; }
    public TokenPriceDto TopBidPrice { get; set; }
    public SeedStatus? NotSupportSeedStatus { get; set; }
    public long AuctionEndTime { get; set; }
    public bool IsBurned { get; set; }
    public int AuctionStatus { get; set; }
    public int BidsCount { get; set; }
    public int BiddersCount { get; set; }
    public int RankingWeight { get; set; }
}