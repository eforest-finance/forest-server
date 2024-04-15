using System;
using NFTMarketServer.Entities;
using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Eto;

[EventName("NFTCollectionTradeEto")]
public class NFTCollectionTradeEto : MultiChainEntity<string>
{
    public override string Id { get; set; }
    public string CollectionId { get; set; }
    public new string ChainId { get; set; }
    
    public long CurrentOrdinal { get; set; }
    public string CurrentOrdinalStr { get; set; }
}