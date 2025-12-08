using System;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.Order;

public class NFTOrderBase: NFTMarketEntity<Guid>
{
    [Keyword] public string NftSymbol { get; set; }
    // example： symbolMarket
    [Keyword] public string MerchantName { get; set; }
    // callback url
    [Keyword] public string WebhookUrl { get; set; }
    [Keyword] public string NftPicture { get; set; }
    [Keyword] public string PaymentSymbol { get; set; }
    public long PaymentAmount { get; set; }
}