using System;
using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Order.Dto;

public class SearchOrderInput
{
    [Required] public Guid OrderId { get; set; }
}