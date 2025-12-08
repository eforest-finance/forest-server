using System.Collections.Generic;

namespace NFTMarketServer;

public static class FileContentHelper
{
    private static List<string> _imageContentTypes = new List<string>
    {
        "image/png", "image/jpeg", "image/gif"
    };
    
    private static List<string> _audioContentTypes = new List<string>
    {
        "audio/mpeg"
    };
    
    private static List<string> _videoContentTypes = new List<string>
    {
        "video/mp4"
    };

    public static bool IsSupportedImage(string contentType)
    {
        return _imageContentTypes.Contains(contentType);
    }
    
    public static bool IsSupportedVideo(string contentType)
    {
        return _videoContentTypes.Contains(contentType);
    }

    public static bool IsSupportedAudio(string contentType)
    {
        return _audioContentTypes.Contains(contentType);
    }
}