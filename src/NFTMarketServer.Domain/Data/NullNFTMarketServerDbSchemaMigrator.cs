using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Data
{
    /* This is used if database provider does't define
     * INFTMarketServerDbSchemaMigrator implementation.
     */
    public class NullNFTMarketServerDbSchemaMigrator : INFTMarketServerDbSchemaMigrator, ITransientDependency
    {
        public Task MigrateAsync()
        {
            return Task.CompletedTask;
        }
    }
}