using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NFTMarketServer.Ai;

public class OpenAiImageGenerationResponse
{
    [JsonPropertyName("created")]
    public long Created { get; set; }
    public List<OpenAiImageGeneration> Data { get; set; }
    
    [JsonPropertyName("error")]
    public OpenAiImageGenerationError Error { get; set; }
}

public class OpenAiImageGeneration
{
    public string Url { get; set; }
}

public class OpenAiImageGenerationError
{
    public string Code { get; set; }
    public string Message { get; set; }
    public string Param { get; set; }
    public string Type { get; set; }
    public string Url { get; set; }
}