using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NFTMarketServer.Ai;

public class OpenAiWordCheckResponse
{
    public List<AIResult> Results { get; set; }
}

public class AIResult
{
    public bool Flagged  { get; set; }
    public Dictionary<string, bool> Categories { get; set; }  
}

public class Category
{
    [JsonPropertyName("sexual")] 
    public bool Sexual { get; set; }
    [JsonPropertyName("hate")]
    public bool Hate { get; set; }
    [JsonPropertyName("harassment")]
    public bool Harassment { get; set; }
    [JsonPropertyName("self-harm")]
    public bool SelfHarm { get; set; }
    [JsonPropertyName("sexual/minors")]
    public bool SexualMinors { get; set; }
    [JsonPropertyName("hate/threatening")]
    public bool HateThreatening { get; set; }
    [JsonPropertyName("violence/graphic")]
    public bool ViolenceGraphic { get; set; }
    [JsonPropertyName("self-harm/intent")]
    public bool SelfHarmIntent { get; set; }
    [JsonPropertyName("self-harm/instructions")]
    public bool SelfHarmInstructions { get; set; }
    [JsonPropertyName("harassment/threatening")]
    public bool HarassmentThreatening { get; set; }
    [JsonPropertyName("violence")]
    public bool Violence { get; set; }
}

