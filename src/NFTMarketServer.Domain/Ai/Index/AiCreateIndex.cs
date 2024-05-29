using System;
using System.Runtime.Serialization;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.Ai.Index;

public class AiCreateIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Address { get; set; }
    [Text(Index = false)] public string Promt { get; set; }
    [Text(Index = false)] public string NegativePrompt { get; set; }
    [Keyword] public AiModelType Model { get; set; }
    [Keyword] public AiQualityType Quality { get; set; }
    [Keyword] public AiStyleType Style { get; set; }
    [Keyword] public AiPaintingStyleType PaintingStyle { get; set; }
    [Keyword] public AiSizeType Size { get; set; }
    public int Number { get; set; }
    public AiCreateStatus Status { get; set; }
    public int RetryCount { get; set; }
    [Keyword] public string Result { get; set; }
    [Keyword] public string TransactionId { get; set; }
    public DateTime Ctime { get; set; }
    public DateTime Utime { get; set; }
    [Text(Index = false)] public string Image { get; set; }
}

public enum AiCreateStatus
{
    INIT,
    PAYSUCCESS,
    IMAGECREATED
}

public enum AiSizeType
{
    [EnumMember(Value = "none")]
    NONE,
    [EnumMember(Value = "256x256")]
    SIZE256x256,
    [EnumMember(Value = "512x512")]
    SIZE512x512,
    [EnumMember(Value = "1024x1024")]
    SIZE1024x1024,
    [EnumMember(Value = "1792x1024")]
    SIZE1792x1024,
    [EnumMember(Value = "1024x1792")]
    SIZE1024x1792
    
}

public enum AiStyleType
{
    [EnumMember(Value = "none")]
    NONE,
    [EnumMember(Value = "vivid")]
    VIVID,
    [EnumMember(Value = "natural")]
    NATURAL
}

public enum AiPaintingStyleType
{
    [EnumMember(Value = "none")]
    NONE,
    [EnumMember(Value = "Pixel")]
    PIXEL,
    [EnumMember(Value = "Cartoon")]
    CARTOON,
    [EnumMember(Value = "Anime")]
    ANIME,
    [EnumMember(Value = "Technology")]
    TECHNOLOGY,
    [EnumMember(Value = "Film")]
    FILM,
    [EnumMember(Value = "Sketch")]
    SKETCH
}

public enum AiModelType
{
    [EnumMember(Value = "none")]
    NONE,
    [EnumMember(Value = "dall-e-2")]
    DALLE2,
    [EnumMember(Value = "dall-e-3")]
    DALLE3
}
public enum AiQualityType
{
    [EnumMember(Value = "none")]
    NONE,
    [EnumMember(Value = "standard")]
    STANDARD,
    [EnumMember(Value = "hd")]
    HD
}