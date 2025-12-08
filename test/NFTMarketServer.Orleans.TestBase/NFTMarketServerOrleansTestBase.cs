using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace NFTMarketServer.Orleans.TestBase;

public abstract class NFTMarketServerOrleansTestBase<TStartupModule> : NFTMarketServerTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    public NFTMarketServerOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}