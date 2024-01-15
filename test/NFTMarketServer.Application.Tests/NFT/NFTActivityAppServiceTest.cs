using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT;

public sealed partial class NFTActivityAppServiceTest : NFTMarketServerApplicationTestBase
{
    private readonly INFTActivityAppService _nftActivityAppService;

    public NFTActivityAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftActivityAppService = GetRequiredService<INFTActivityAppService>();
    }


    [Fact]
    public async Task GetNftActivityListAsyncTest()
    {
        var input = new GetActivitiesInput();
        input.NFTInfoId = "AElf-QWE-1";
        input.Types = new List<int> { 2, 3 };
        var res = await _nftActivityAppService.GetListAsync(input);
        res.TotalCount.ShouldBe(1);
        res.Items[0].From.Address.ShouldNotBeNull();
    }
}