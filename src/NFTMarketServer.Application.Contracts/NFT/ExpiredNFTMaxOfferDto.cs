using System;
using System.Collections.Generic;

namespace NFTMarketServer.NFT;


public class ExpiredNftMaxOfferInfo
{
    public string Id  { get; set; }
    public DateTime ExpireTime { get; set; }
    public decimal Prices  { get; set; }
}

public class ExpiredNftMaxOfferDto
{
    public string Key { get; set; }
    public ExpiredNftMaxOfferInfo Value { get; set; }
}

public class ExpiredNftMaxOfferResultDto
{
    public List<ExpiredNftMaxOfferDto> GetExpiredNftMaxOffer{ get; set; }
}

