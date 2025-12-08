using Volo.Abp.Domain.Entities;

namespace NFTMarketServer.NFT.Index;

public abstract class NFTMarketServerEsEntity<TKey> : Entity, IEntity<TKey>
{
    public virtual TKey Id { get; set; }

    public override object[] GetKeys()
    {
        return new object[] { Id };
    }
}