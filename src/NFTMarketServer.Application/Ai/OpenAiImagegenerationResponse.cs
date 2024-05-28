using System.Collections.Generic;

namespace NFTMarketServer.Ai;

public class OpenAiImageGenerationResponse
{
    public long Created { get; set; }
    public List<OpenAiImageGeneration> Data { get; set; }
    public OpenAiImageGenerationError Error { get; set; }
}

public class OpenAiImageGeneration
{
    public string Url { get; set; }
}

public class OpenAiImageGenerationError
{
    public int Code { get; set; }
    public string Message { get; set; }
    public string Param { get; set; }
    public string Type { get; set; }
    public string Url { get; set; }
}