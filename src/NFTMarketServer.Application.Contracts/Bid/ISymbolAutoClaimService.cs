using System.Threading.Tasks;

namespace NFTMarketServer.Bid;

public interface ISymbolAutoClaimService
{
    Task SyncSymbolClaimAsync();
}