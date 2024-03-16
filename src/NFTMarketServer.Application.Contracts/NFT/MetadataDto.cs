using Newtonsoft.Json;

namespace NFTMarketServer.NFT
{
    public class MetadataDto
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    
    public class TickDto
    {
        [JsonProperty(PropertyName = "lim")]
        public string Lim { get; set; }
        [JsonProperty(PropertyName = "tick")]
        public string Tick { get; set; }
    }
}