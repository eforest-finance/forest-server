using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.OwnerShip.Dto;

public class AutoVerifyInput
{
    [Required] public string Message { get; set; }
    [Required] public string Signature { get; set; }
}

public class MessageInfo
{
    public string Symbol { get; set; }
    public string IssueContractAddress { get; set; }
    public string From { get; set; }
    public string ChainId { get; set; }
    public string IssueAddress { get; set; }
}