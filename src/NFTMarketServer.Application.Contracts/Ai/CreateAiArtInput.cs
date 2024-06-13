using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Ai;

public class CreateAiArtInput
{
    [NotNull]public string RawTransaction { get; set; }
    public string Describe { get; set; }
    public string PublicKey { get; set; }
    [NotNull]public string ChainId { get; set; }
}

public class CreateAiResultDto
{
    public bool CanRetry { get; set; }
    public string TransactionId { get; set; }
    
    public int TotalCount{ get; set; }

    public List<CreateAiArtDto> itms { get; set; }
    
    public bool Success { get; set; }
    
    public string ErrorMsg { get; set; }

}



public class CreateAiArtDto
{
    public string Url { get; set; }
    public string Hash { get; set; }
}