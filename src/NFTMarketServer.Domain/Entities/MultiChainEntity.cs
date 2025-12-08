namespace NFTMarketServer.Entities
{
    public class MultiChainEntity<TKey> : NFTMarketEntity<TKey>, IMultiChain
    {
        public virtual int ChainId { get; set; }


        protected MultiChainEntity()
        {
        }
    }
}