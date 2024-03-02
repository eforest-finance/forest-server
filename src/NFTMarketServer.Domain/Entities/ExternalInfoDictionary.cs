using Nest;
using Newtonsoft.Json;

namespace NFTMarketServer.Entities;

public class ExternalInfoDictionary
{
    [JsonProperty(PropertyName = "key")]
    [Keyword] public string Key { get; set; }
    [JsonProperty(PropertyName = "value")]
    [Keyword] public string Value { get; set; }
}