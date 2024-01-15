using System.Threading.Tasks;

namespace NFTMarketServer.Bid;

public interface ISymbolAuctionScheduleService
{

    Task SyncSymbolAuctionRecordsAsync(string chainId);

    Task SyncSymbolBidRecordsAsync(string chainId);

}