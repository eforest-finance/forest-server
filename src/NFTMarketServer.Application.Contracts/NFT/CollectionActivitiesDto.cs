

namespace NFTMarketServer.NFT
{
    public class CollectionActivitiesDto : NFTActivityDto
    {
        public string NFTName { get; set; }
        public string NFTCollectionName { get; set; }
        public string PreviewImage { get; set; }
        
        public int Rank { get; set; }
        public string Level { get; set; }
        public string Grade { get; set; }
        public string Star{ get; set; }
        public string Rarity { get; set; }
        public string Describe { get; set; }
    }
}