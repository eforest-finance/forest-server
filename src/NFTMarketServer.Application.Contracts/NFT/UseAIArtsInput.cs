using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class UseAIArtsInput 
{
    public List<string> ImageList { get; set; }
    public int Status { get; set; }

}
public enum AiImageUseStatus
{
    UNUSE,
    USE,
    ALL,
    ABANDONED
}