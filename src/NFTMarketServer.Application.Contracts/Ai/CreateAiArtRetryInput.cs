using System.Diagnostics.CodeAnalysis;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Ai;

public class CreateAiArtRetryInput
{
    [NotNull]
    public string TransactionId { get; set; }
}
