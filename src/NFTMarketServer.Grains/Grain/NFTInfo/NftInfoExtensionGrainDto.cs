
namespace NFTMarketServer.Grains.Grain.NFTInfo;
[GenerateSerializer]
public class NftInfoExtensionGrainDto
{
    [Id(0)]
    public string Id    { get; set; }
    [Id(1)]
    public string ChainId { get; set; }
    [Id(2)]
    public string NFTSymbol { get; set; }
    [Id(3)]
    public string PreviewImage { get; set; }
    [Id(4)]
    public string File { get; set; }
    [Id(5)]
    public string FileExtension { get; set; }
    [Id(6)]
    public string Description { get; set; }
    [Id(7)]
    public string TransactionId { get; set; }

    [Id(8)]
    public string OldFile { get; set; }

    [Id(9)]
    public string ExternalLink { get; set; }

    [Id(10)]
    public string CoverImageUrl { get; set; }

}