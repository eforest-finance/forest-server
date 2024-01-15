using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Users;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public sealed partial class NftListingAppServiceTest
{
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetListingProvider());
        services.AddSingleton(GetMockUserService());
    }

    private INFTListingProvider GetListingProvider()
    {
        var listing1 = new IndexerNFTListingInfo()
        {
            Symbol = "TEST-1",
            Owner = "userAddress1",
            WhitelistId = "test-whitelist-id",
            StartTime = DateTime.UtcNow,
            PublicTime = DateTime.UtcNow,
            ExpireTime = DateTime.UtcNow,
            Quantity = 1,
            Prices = 10,
            NftCollectionDto = new IndexerNFTCollection
            {
                CreatorAddress = "userAddress1",
                Id = "AELF-TEST-0",
                TokenName = "TEST-0",
                Symbol = "TEST-0",
            },
            NftInfo = new IndexerNFTInfo
            {
                Symbol = "TEST-1",
            }
        };
        
        var mockProvider = new Mock<INFTListingProvider>();

        var dto = new GetNFTListingsDto()
        {
            ChainId = "aa",
            Symbol = "bb",
            Address = "cc",
            SkipCount = 0,
            MaxResultCount = 1
        };
        
        mockProvider
            .Setup(provider => provider.GetNFTListingsAsync(It.Is<GetNFTListingsDto>(dto => dto.Symbol == "TEST-1" && 
                dto.SkipCount == 0 && dto.MaxResultCount == 10)))
            .ReturnsAsync(new PagedResultDto<IndexerNFTListingInfo>()
            {
                TotalCount = 1,
                Items = new List<IndexerNFTListingInfo> { listing1 }
            });
        return mockProvider.Object;
    }

    private IUserAppService GetMockUserService()
    {
        var user = new AccountDto()
        {
            Name = "userAddress1",
            Address = "userAddress1"
        };
        var mockProvider = new Mock<IUserAppService>();
        mockProvider
            .Setup(provider => provider.GetAccountsAsync(
                It.Is<List<string>>(addresses => addresses.Contains("userAddress1")),It.IsAny<string>()))
            .ReturnsAsync(new Dictionary<string, AccountDto>
            {
                ["userAddress1"] = user
            });
        return mockProvider.Object;
    }
}