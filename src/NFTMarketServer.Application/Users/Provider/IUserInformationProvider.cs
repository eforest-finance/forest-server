
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Users.Dto;
using NFTMarketServer.Users.Index;

namespace NFTMarketServer.Users.Provider;

public interface IUserInformationProvider
{
    public Task<bool> CheckNameAsync(string name, Guid? userId);
    
    public Task<UserDto> SaveUserSourceAsync(UserSourceInput userSourceInput);

    public Task<UserIndex> GetByUserAddressAsync(string inputAddress);
    
    public Task<List<string>> GetFullAddressAsync(string inputAddress);
    
    public Task<List<string>> GetAllAddressAsync(string inputAddress);

    public Task<UserIndex> GetByIdAsync(Guid id);
    
    public Task AddNewUserCountAsync(UserSourceInput userSourceInput, string userName);

}