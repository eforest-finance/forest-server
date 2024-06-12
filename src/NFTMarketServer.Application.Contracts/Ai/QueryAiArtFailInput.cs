
namespace NFTMarketServer.Ai;

public class QueryAiArtFailInput
{
   
}

public class AiArtFailDto
{
    public string Prompt;
    public string NegativePrompt;
    public string AiPaintingStyleType { get; set; }
    public string Size { get; set; }
    public string Quality { get; set; }
    public int Number { get; set; }
    public string TransactionId { get; set; }
}
