using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.Users.Index;

namespace NFTMarketServer.Users.Provider;

public interface IUserBalanceProvider
{
    public Task<Tuple<long, List<UserBalanceIndex>>> GetUserBalancesAsync(string address, QueryUserBalanceListInput input);

    public Task SaveOrUpdateUserBalanceAsync(UserBalanceDto userBalanceDto);
    
    public Task BatchSaveOrUpdateUserBalanceAsync(List<UserBalanceIndex> userBalanceIndices);
    
    public Task<Tuple<long, List<UserBalanceIndex>>> GetUserBalancesAsync(QueryUserBalanceIndexInput input);

    public Task<List<UserBalanceIndex>> GetValidUserBalanceInfosAsync(QueryUserBalanceIndexInput input);


}