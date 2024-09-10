using System.Diagnostics.CodeAnalysis;

namespace NFTMarketServer.Platform;

public class CreatePlatformNFTInput
{
    [NotNull]public string NFTUrl { get; set; }
    [NotNull]public string NFTName { get; set; }
}
