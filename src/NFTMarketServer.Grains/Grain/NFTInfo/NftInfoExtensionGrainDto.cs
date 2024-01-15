
namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class NftInfoExtensionGrainDto
{
    public string Id    { get; set; }
    public string ChainId { get; set; }
    public string NFTSymbol { get; set; }
    public string PreviewImage { get; set; }
    public string File { get; set; }
    public string FileExtension { get; set; }
    public string Description { get; set; }
    public string TransactionId { get; set; }

    public string OldFile { get; set; }
            
    public string ExternalLink { get; set; } 
    
    public string CoverImageUrl { get; set; }

}