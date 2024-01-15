using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.Seed.Dto;

public class SeedInfoDto : IndexerCommonResult<SeedInfoDto>
{
    public string Id { get; set; }

    public string Symbol { get; set; }

    public string SeedSymbol { get; set; }

    public string SeedName { get; set; }

    public SeedStatus Status { get; set; }

    public long RegisterTime { get; set; }

    public long ExpireTime { get; set; }

    public string TokenType { get; set; }

    public SeedType SeedType { get; set; }
    
    public AuctionType AuctionType { get; set; }

    public PriceInfo TokenPrice { get; set; }

    public string SeedImage { get; set; }
    public string Owner { get; set; }
    public string CurrentChainId { get; set; }
    public TokenPriceDto TopBidPrice { get; set; }
    public SeedStatus? NotSupportSeedStatus { get; set; }
    public long AuctionEndTime { get; set; }
    public bool IsBurned { get; set; }
}

public class PriceInfo
{
    public string Symbol { get; set; }
    public long Amount { get; set; }
}

public class SearchSeedInfoResultDto
{
    public SeedInfoDto SearchSeedInfo { get; set; }
}