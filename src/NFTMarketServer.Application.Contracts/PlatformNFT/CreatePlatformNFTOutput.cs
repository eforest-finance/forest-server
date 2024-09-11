using System.Diagnostics.CodeAnalysis;

namespace NFTMarketServer.Platform;

public class CreatePlatformNFTOutput
{
    public string CollectionSymbol { get; set; }
    public string CollectionId { get; set; }
    public string NFTSymbol { get; set; }
    public string NFTId { get; set; }
    
    public string CollectionIcon{ get; set; }
    public string CollectionName{ get; set; }

}
