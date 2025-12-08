using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace NFTMarketServer.MongoDB;

[ConnectionStringName("Default")]
public class NFTMarketServerMongoDbContext : AbpMongoDbContext
{
}