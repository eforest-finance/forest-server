using System;

namespace NFTMarketServer.NFT.Dtos;

public class NFTMessageActivityDto
{
    public string Id { get; set; }
    public string NFTInfoId { get; set; }
    public NFTActivityType Type { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public long Amount { get; set; }
    public TokenInfoDto PriceTokenInfo { get; set; }
    public decimal Price { get; set; }
    public string TransactionHash { get; set; }
    public DateTime Timestamp { get; set; }
    public long BlockHeight { get; set;}
}

public class TokenInfoDto
{
    public string Id { get; set; }

    public string ChainId { get; set; }

    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    public string PreviousBlockHash { get; set; }

    public string Symbol { get; set; }

    /// <summary>
    ///     token contract address
    /// </summary>
    public string TokenContractAddress { get; set; }

    public int Decimals { get; set; }

    public long TotalSupply { get; set; }

    public string TokenName { get; set; }

    public string Issuer { get; set; }

    public bool IsBurnable { get; set; }

    public int IssueChainId { get; set; }
}