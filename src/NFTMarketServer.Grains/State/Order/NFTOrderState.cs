using NFTMarketServer.Order;

namespace NFTMarketServer.Grains.State.Order;

public class NFTOrderState : NFTOrder
{
    public Dictionary<OrderStatus, OrderStatusInfo> OrderStatusInfoMap { get; set; }
}

public class OrderStatusInfo
{
    public OrderStatus OrderStatus { get; set; }
    public long Timestamp { get; set; }
    public Dictionary<string, string> ExternalInfo { get; set; }
}