using System;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class NFTActivityDto: EntityDto<string>
{
    public string NFTInfoId { get; set; }
    public NFTActivityType Type { get; set; }
    public AccountDto From { get; set; }
    public AccountDto To { get; set; }
    public long Amount { get; set; }
    public TokenDto PriceToken { get; set; }
    public decimal Price { get; set; }
    public string TransactionHash { get; set; }
    public long Timestamp { get; set; }
    
    public string Symbol{ get; set; }
    public string CollectionSymbol{ get; set; }
    public string CollectionName{ get; set; }
    
    public decimal TotalPrice{ get; set; }
    
    public string NFTUrl{ get; set; }
    
    public string NFTName{ get; set; }

}