using System;
using AElf.Indexing.Elasticsearch;

namespace NFTMarketServer.NFT.Index;

public class UserBalanceIndex : UserBalanceBase, IIndexBuild
{
    public decimal ListingPrice { get; set; }
    public DateTime? ListingTime { get; set; }
    public BalanceType BalanceType { get; set; } = BalanceType.Other;
}

public enum BalanceType
{
    Elf,
    Nft,
    Other
}