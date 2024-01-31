using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Eto;

[EventName("NFTDropExtraEto")]
public class NFTDropExtraEto
{
    public string DropId { get; set; }
    public string DropName { get; set; }
    public string Introduction { get; set; }
        
    public string BannerUrl { get; set; }
    public string LogoUrl { get; set; }
    public string TransactionId { get; set; }
    public List<SocialMedia> SocialMedia { get; set; }
}