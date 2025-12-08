using Newtonsoft.Json;

namespace NFTMarketServer.Ai;

public class OpenAiWordCheckDto
{
    [JsonProperty("input")] public string Input { get; set; }
}