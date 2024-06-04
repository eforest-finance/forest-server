using System.ComponentModel;
using Newtonsoft.Json;

namespace NFTMarketServer.Ai;

public class OpenAiImageGenerationDto
{
    [JsonProperty("model")] public string Model { get; set; }
    [JsonProperty("prompt")] public string Prompt { get; set; }
    [JsonProperty("n")] public int N { get; set; }

    [JsonProperty("size"), Description("256x256, 512x512, or 1024x1024")]
    public string Size { get; set; }
}