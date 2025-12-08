using System.Threading.Tasks;

namespace NFTMarketServer.Seed;

public interface ISeedIconAppService
{
    Task SyncSeedIconRecordsAsync(string chainId);
}