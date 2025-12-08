using System.Threading.Tasks;

namespace NFTMarketServer.Data
{
    public interface INFTMarketServerDbSchemaMigrator
    {
        Task MigrateAsync();
    }
}
