using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Bid.Dtos;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Bid;

public interface IBidAppService
{
    Task<PagedResultDto<BidInfoDto>> GetSymbolBidInfoListAsync(GetSymbolBidInfoListRequestDto input);
    
    
    Task<BidInfoDto> GetSymbolBidInfoAsync(string symbol,string transactionHash);

    Task<List<AuctionInfoDto>> GetSymbolAuctionInfoListAsync(string seedSymbol);
    
    Task<AuctionInfoDto> GetSymbolAuctionInfoAsync(string seedSymbol);



    Task<List<AuctionInfoDto>> GetUnFinishedSymbolAuctionInfoListAsync(GetAuctionInfoRequestDto input);
    
    Task<AuctionInfoDto> GetSymbolAuctionInfoByIdAsync(string auctionId);
    
    Task<AuctionInfoDto> GetSymbolAuctionInfoByIdAndTransactionHashAsync(string auctionId,string transactionHash);

    Task AddSymbolAuctionInfoListAsync(AuctionInfoDto auctionInfoDto);
    
    
    Task UpdateSymbolAuctionInfoAsync(AuctionInfoDto auctionInfoDto);
    
    Task AddBidInfoListAsync(BidInfoDto auctionInfoDto);


    Task<SeedAuctionInfoDto> GetSeedAuctionInfoAsync(string symbol);
}