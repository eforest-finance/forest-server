using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Users;

namespace NFTMarketServer.NFT;

public sealed partial class NFTActivityAppServiceTest : NFTMarketServerApplicationTestBase
{
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(getNFTActivityProvider());
        services.AddSingleton(GetMockUserService());
        base.AfterAddApplication(services);
    }

    public INFTActivityProvider getNFTActivityProvider()
    {
        var NFTActivity = new NFTActivityItem()
        {
            NFTInfoId = "AELF-QWE-1",
            Amount = 2,
            TransactionHash = "123213213",
            From = "userAddress",
            To = "aelfAddress",
            Type = NFTActivityType.Mint
        };
        var mockProvider = new Mock<INFTActivityProvider>();
        mockProvider.Setup(provider => provider.GetNFTActivityListAsync(
                It.IsAny<string>(),
                It.IsAny<List<int>>(),
                It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new IndexerNFTActivityPage()
            {
                TotalRecordCount = 1,
                IndexerNftActivity = new List<NFTActivityItem>
                {
                    NFTActivity
                }
            });
        return mockProvider.Object;
    }

    private IUserAppService GetMockUserService()
    {
        var userAddress = new AccountDto()
        {
            Name = "userAddress",
            Address = "userAddress"
        };
        var aelfAddress = new AccountDto()
        {
            Name = "aelfAddress",
            Address = "aelfAddress"
        };

        var mockProvider = new Mock<IUserAppService>();
        mockProvider
            .Setup(provider => provider.GetAccountsAsync(
                It.IsAny<List<string>>(),It.IsAny<string>()))
            .ReturnsAsync(new Dictionary<string, AccountDto>
            {
                ["userAddress"] = userAddress,
                ["aelfAddress"] = aelfAddress
            });
        return mockProvider.Object;
    }
}