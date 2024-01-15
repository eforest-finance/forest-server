namespace NFTMarketServer.Market
{
    public class CancelNFTOfferInput:InputBase
    {
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public string OfferFrom { get; set; }
        public string OfferTo { get; set; }
        // TODO: 
        //Int32List index_list = 5;
    }
}