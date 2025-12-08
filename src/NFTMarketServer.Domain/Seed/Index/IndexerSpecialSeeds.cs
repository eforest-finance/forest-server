using System.Collections.Generic;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Seed.Dto;
using TokenType = NFTMarketServer.Seed.Dto.TokenType;

namespace NFTMarketServer.Seed.Index;

public class IndexerSpecialSeeds: IndexerCommonResult<IndexerSpecialSeeds>
{
    public long TotalRecordCount { get; set; }

    public List<SpecialSeedItem> IndexerSpecialSeedList { get; set; }
}

public class SpecialSeedItem
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string SeedSymbol { get; set; }
    public string SeedName { get; set; }
    public string SeedImage { get; set; }
    public SeedStatus Status { get; set; }
    public TokenType TokenType { get; set; }
    public SeedType SeedType { get; set; }
    public AuctionType AuctionType { get; set; }
    public TokenPriceDto TokenPrice { get; set; }
}