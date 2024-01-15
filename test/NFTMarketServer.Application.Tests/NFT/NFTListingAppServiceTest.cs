using Newtonsoft.Json;
using NFTMarketServer.Market;
using NFTMarketServer.Users;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT;

public sealed partial class NftListingAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly INFTListingAppService _nftListingAppService;
    private readonly IUserAppService _userAppService;

    public NftListingAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _userAppService = GetRequiredService<IUserAppService>();
        _nftListingAppService = GetRequiredService<INFTListingAppService>();
    }

    [Fact]
    public async void GetListingTest()
    {
        var res = await _nftListingAppService.GetNFTListingsAsync(new GetNFTListingsInput()
        {
            ChainId = "AELF",
            Symbol = "TEST-1",
            MaxResultCount = 10,
            SkipCount = 0
        });
        TestOutputHelper.WriteLine(JsonConvert.SerializeObject(res));
        res.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async void GetListingTes_symboInvalid()
    {
        var res = () => _nftListingAppService.GetNFTListingsAsync(new GetNFTListingsInput()
        {
            ChainId = "AELF",
            Symbol = "NFT-1?",
            MaxResultCount = 10,
            SkipCount = 0
        });

        var exception = await Assert.ThrowsAsync<AbpValidationException>(res);
        TestOutputHelper.WriteLine(JsonConvert.SerializeObject(exception));
        exception.ShouldNotBeNull();
        exception.ValidationErrors.Count.ShouldBeGreaterThan(0);
        exception.ValidationErrors[0].ErrorMessage.ShouldContain("Symbol invalid");
    }
    
    [Fact]
    public async void GetListingTes_chainIdInvalid()
    {
        var res = () => _nftListingAppService.GetNFTListingsAsync(new GetNFTListingsInput()
        {
            ChainId = "AELF=",
            Symbol = "NFT-1",
            MaxResultCount = 10,
            SkipCount = 0
        });

        var exception = await Assert.ThrowsAsync<AbpValidationException>(res);
        TestOutputHelper.WriteLine(JsonConvert.SerializeObject(exception));
        exception.ShouldNotBeNull();
        exception.ValidationErrors.Count.ShouldBeGreaterThan(0);
        exception.ValidationErrors[0].ErrorMessage.ShouldContain("ChainId invalid");
    }
    
}