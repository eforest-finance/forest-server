using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Order.Dto;

public class CreateOrderInput
{
    [Required] public string Symbol { get; set; }
    public string Type { get; set; }
}