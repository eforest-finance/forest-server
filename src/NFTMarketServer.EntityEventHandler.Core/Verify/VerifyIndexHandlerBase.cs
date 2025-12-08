using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.EntityEventHandler.Core.Verify
{
    public class VerifyIndexHandlerBase : ITransientDependency
    {
        public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
        protected IObjectMapper ObjectMapper => LazyServiceProvider.LazyGetRequiredService<IObjectMapper>();
        protected IDistributedEventBus DistributedEventBus => LazyServiceProvider.LazyGetRequiredService<IDistributedEventBus>();

    }
}