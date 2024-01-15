using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Synchronize.Dto;

public class GetSyncResultByTxHashDto
{
    // public Guid UserId { get; set; }
    [Required] public string TxHash { get; set; }
}

public class SendNFTSyncDto
{
    // public Guid UserId { get; set; }
    [Required] public string TxHash { get; set; }
    [Required] public string Symbol { get; set; }
    [Required] public string FromChainId { get; set; }
    [Required] public string ToChainId { get; set; }
}