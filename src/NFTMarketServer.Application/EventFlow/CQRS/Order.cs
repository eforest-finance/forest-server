using System;
using Volo.Abp.Domain.Entities;

namespace NFTMarketServer.EventFlow.CQRS;

public class Order : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public object[] GetKeys()
    {
        throw new NotImplementedException();
    }
}