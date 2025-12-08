using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.OwnerShip.Dto;

public class CommitManualVerifyInput
{
    [Required] public string IssueChain { get; set; }
    [Required] public string ProjectCreatorAddress { get; set; }
    [Required] public string Message { get; set; }
    [Required] public string Signature { get; set; }
    [Required] public string Proof { get; set; }
}