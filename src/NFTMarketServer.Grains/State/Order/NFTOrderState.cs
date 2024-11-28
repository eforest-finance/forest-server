using NFTMarketServer.Order;

namespace NFTMarketServer.Grains.State.Order;

public class NFTOrderState : NFTOrder
{
    public Dictionary<OrderStatus, OrderStatusInfo> OrderStatusInfoMap { get; set; }
}
[GenerateSerializer]
public class OrderStatusInfo
{
    [Id(0)]
    public OrderStatus OrderStatus { get; set; }
    [Id(1)]
    public long Timestamp { get; set; }
    [Id(2)]
    public Dictionary<string, string> ExternalInfo { get; set; }
}