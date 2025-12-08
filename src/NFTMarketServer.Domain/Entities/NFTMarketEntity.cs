using System;
using Volo.Abp.Domain.Entities;

namespace NFTMarketServer.Entities
{
    /// <inheritdoc cref="IEntity" />
    [Serializable]
    public abstract class NFTMarketEntity<TKey> : Entity, IEntity<TKey>
    {
        /// <inheritdoc/>
        public virtual TKey Id { get; set; }

        protected NFTMarketEntity()
        {

        }

        protected NFTMarketEntity(TKey id)
        {
            Id = id;
        }

        public override object[] GetKeys()
        {
            return new object[] {Id};
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[ENTITY: {GetType().Name}] Id = {Id}";
        }
    }
}