using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using NFTMarketServer.Users.Dto;
using NFTMarketServer.Users.Index;
using NSubstitute;
using Shouldly;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Users.Provider;

public class UserInformationProviderTest : NFTMarketServerApplicationTestBase
{
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IUserAppService _userAppService;
    private readonly INESTRepository<UserIndex, Guid> _userIndexRepository;
    private ICurrentUser _currentUser; 
    private readonly INESTRepository<UserExtraIndex, Guid> _userExtraIndexRepository;
    
    public UserInformationProviderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _userInformationProvider = GetRequiredService<IUserInformationProvider>();
        _userAppService = GetRequiredService<IUserAppService>();
        _userIndexRepository = GetRequiredService<INESTRepository<UserIndex, Guid>>();
        _userExtraIndexRepository = GetRequiredService<INESTRepository<UserExtraIndex, Guid>>();
    }
    protected override void AfterAddApplication(IServiceCollection services)
    {
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
    }

    [Fact]
    public async Task AddAndUpdateUserInformationTest()
    {
        Guid userId = Guid.NewGuid();
        Login(userId);
        UserSourceInput userSourceInput = new UserSourceInput
        {
            UserId = userId,
            AelfAddress = "aelfadresstest"
        };

        // test normal 
        await _userInformationProvider.SaveUserSourceAsync(userSourceInput);
        Dictionary<string, string> CaAddressSide = new Dictionary<string, string>()
        {
            ["TDVW"] = "caSide"
        };

        userSourceInput = new UserSourceInput
        {
            UserId = userId,
            AelfAddress = "aelfadresstest",
            CaHash = "cahashtest",
            CaAddressMain = "caaddressmaintest",
            CaAddressSide = CaAddressSide
        };
        // test update 
        await _userInformationProvider.SaveUserSourceAsync(userSourceInput);
        UserUpdateDto userUpdateDto = new UserUpdateDto
        {
            Name = "utTest",
            Email = "ut@aelf.io",
            Twitter = "https://twitter.com/status/12",
            Instagram = "https://www.instagram.com/usernam",
            ProfileImage = "http://www.test.com/detail/gO6Qk.html",
            ProfileImageOriginal = "http://www.test.com/detail/gO6Qk.html",
            BannerImage = "http://www.test.com/detail/gO6Qk.html"
        };

        await _userAppService.UserUpdateAsync(userUpdateDto);

        var userDtoNew = await _userAppService.GetByUserAddressAsync(userSourceInput.AelfAddress);
        userDtoNew.Name.ShouldBe(userUpdateDto.Name);
        userDtoNew.Email.ShouldBe(userUpdateDto.Email);
        userDtoNew.Twitter.ShouldBe(userUpdateDto.Twitter);
        userDtoNew.Instagram.ShouldBe(userUpdateDto.Instagram);
        userDtoNew.ProfileImage.ShouldBe(userUpdateDto.ProfileImage);
        userDtoNew.ProfileImageOriginal.ShouldBe(userUpdateDto.ProfileImageOriginal);
        userDtoNew.BannerImage.ShouldBe(userUpdateDto.BannerImage);
        userDtoNew.Address.ShouldBe(userSourceInput.AelfAddress);
        userDtoNew.FullAddress.ShouldContain(userSourceInput.AelfAddress);
        Guid userSecondId = Guid.NewGuid();
        Login(userSecondId);
        
        // test Exception
        try
        {
            UserUpdateDto userUpdateSecondDto = new UserUpdateDto
            {
                Name = userUpdateDto.Name
            };
            await _userAppService.UserUpdateAsync(userUpdateDto);
        }
        catch (Exception e)
        {
            e.Message.ShouldContain("The name already used.");
        }

        var userMap = await _userAppService.GetAccountsAsync(new List<string> { userSourceInput.AelfAddress });
        userMap[userSourceInput.AelfAddress].Address.ShouldBe(userSourceInput.AelfAddress);
        userMap[userSourceInput.AelfAddress].Name.ShouldBe(userUpdateDto.Name);
        userMap[userSourceInput.AelfAddress].Email.ShouldBe(userUpdateDto.Email);
        userMap[userSourceInput.AelfAddress].Twitter.ShouldBe(userUpdateDto.Twitter);
        userMap[userSourceInput.AelfAddress].Instagram.ShouldBe(userUpdateDto.Instagram);
        userMap[userSourceInput.AelfAddress].ProfileImage.ShouldBe(userUpdateDto.ProfileImage);

    }

    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    [Fact]
    public async Task AddUserByCaSideTest()
    {
        Guid userId = Guid.NewGuid();
        Login(userId);
        String addressTdvv = "addressTDVV";
        String addressTdvw = "addressTDVW";
        Dictionary<string, string> CaAddressSide = new Dictionary<string, string>()
        {
            ["TDVV"] = addressTdvv,
            ["TDVW"] = addressTdvw
        };
        UserSourceInput userSourceInput = new UserSourceInput
        {
            UserId = userId,
            CaAddressSide = CaAddressSide
        };

        // test normal 
        await _userInformationProvider.SaveUserSourceAsync(userSourceInput);
        UserUpdateDto userUpdateDto = new UserUpdateDto
        {
            Name = "utTest",
            Email = "ut@aelf.io",
            Twitter = "https://twitter.com/status/12",
            Instagram = "https://www.instagram.com/usernam",
            ProfileImage = "http://www.test.com/detail/gO6Qk.html",
            ProfileImageOriginal = "http://www.test.com/detail/gO6Qk.html",
            BannerImage = "http://www.test.com/detail/gO6Qk.html"
        };

        await _userAppService.UserUpdateAsync(userUpdateDto);
        UserDto userDtoNew = await _userAppService.GetByUserAddressAsync(addressTdvv);
        userDtoNew.Name.ShouldBe(userUpdateDto.Name);
        userDtoNew.Email.ShouldBe(userUpdateDto.Email);
        userDtoNew.BannerImage.ShouldBe(userUpdateDto.BannerImage);
        userDtoNew.Address.ShouldBe(addressTdvv);
        userDtoNew.FullAddress.ShouldContain(addressTdvv);
        Dictionary<string, AccountDto> UserMap = await _userAppService.GetAccountsAsync(new List<string>
        {
            addressTdvv, addressTdvw
        });
        UserMap[addressTdvv].ShouldNotBeNull();
        UserMap[addressTdvw].ShouldNotBeNull();
    }

    [Fact]
    public async Task GetUserCountTest()
    {
        var timeEnd = DateTime.UtcNow;
        var timeBegin = timeEnd.AddHours(-21);

        var ret = await _userAppService.GetUserCountAsync(timeBegin, timeEnd);
        ret.ShouldBe(0);
    }

}