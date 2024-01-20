using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT;

public class NFTForSaleDto
{
    public string Id { get; set; }

    public string ChainId { get; set; }

    public string Symbol { get; set; }

    public string TokenName { get; set; }

    public string CollectionSymbol { get; set; }

    public string CollectionName { get; set; }

    public string LogoImage { get; set; }

    public long ItemTotal { get; set; }

    public long OwnerTotal { get; set; }

    public decimal FloorPrice { get; set; }

    public string FloorPriceSymbol { get; set; }

    public decimal LastDealPrice { get; set; }

    public string LastDealPriceSymbol { get; set; }
    
    public decimal MaxOfferPrice { get; set; }

    public string MaxOfferPriceSymbol { get; set; }
    
    public long AvailableQuantity { get; set; }

    public void OfDtoInfo(IndexerNFTInfo nftInfoIndex, IndexerNFTDealInfo lastDealInfo)
    {
        Id = nftInfoIndex.Id;
        Symbol = nftInfoIndex.Symbol;
        TokenName = nftInfoIndex.TokenName;
        CollectionSymbol = nftInfoIndex.CollectionSymbol;
        LogoImage = nftInfoIndex.ImageUrl;
        LastDealPrice = lastDealInfo != null ? FTHelper.GetRealELFAmount(lastDealInfo.PurchaseAmount) : -1.0m;
        LastDealPriceSymbol = lastDealInfo != null ? lastDealInfo.PurchaseSymbol : CommonConstant.Coin_ELF;
    }
}