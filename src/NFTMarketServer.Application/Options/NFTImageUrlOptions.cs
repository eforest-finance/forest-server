using System.Collections.Generic;

namespace NFTMarketServer.Options;

public class NFTImageUrlOptions
{
    public Dictionary<string, string> ImageUrls { get; set; } = new Dictionary<string, string>();

    public string GetImageUrl(string symbol)
    {
        ImageUrls.TryGetValue(symbol, out var imageUrl);
        return imageUrl;
    }
}