using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Users.Dto;

namespace NFTMarketServer.Users
{
    public interface IUserAppService
    {
        Task<Dictionary<string,AccountDto>> GetAccountsAsync(List<string> addresses,
            string defaultChainId = null);
        Task<bool> CheckNameAsync(string name);
        Task UserUpdateAsync(UserUpdateDto input);
        Task<UserDto> GetByUserAddressAsync(string inputAddress);
        Task<long> GetUserCountAsync(DateTime beginTime, DateTime endTIme);

        Task<string> GetCurrentUserAddressAsync();
        Task<string> TryGetCurrentUserAddressAsync();
    }
}