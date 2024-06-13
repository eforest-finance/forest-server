
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Ai;

public class QueryAiArtFailInput : PagedAndSortedResultRequestDto
{
   
}

public class AiArtFailDto
{
    public string Id { get; set; }
    public string Prompt { get; set; }
    public string NegativePrompt { get; set; }
    public string AiPaintingStyleType { get; set; }
    public string Size { get; set; }
    public string Quality { get; set; }
    public int Number { get; set; }
    public string TransactionId { get; set; }
    
    public int RetryCount { get; set; }
    
    public string Result { get; set; }
    
    public string Address { get; set; }
}
