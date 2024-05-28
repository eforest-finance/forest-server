using System.Diagnostics.CodeAnalysis;

namespace NFTMarketServer.Ai;

public class CreateAiArtInput
{
    [NotNull]public string RawTransaction { get; set; }
    public string Describe { get; set; }
    public string PublicKey { get; set; }
    [NotNull]public string ChainId { get; set; }
}