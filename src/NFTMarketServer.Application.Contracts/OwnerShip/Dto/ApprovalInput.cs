using System;
using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.OwnerShip.Dto;

public class ApprovalInput
{
    [Required] public Guid Id { get; set; }
    public string Comment { get; set; }
}