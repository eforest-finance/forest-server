using System.Threading.Tasks;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Market;

public class CommonTest : NFTMarketServerApplicationTestBase
{
    public CommonTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        
    }

    [Fact]
    public async Task NFTHelperTest()
    {
        var createInputSymbol1Type = NFTHelper.GetCreateInputSymbolType("FJPEUKMTTO-0");
        createInputSymbol1Type.ShouldBe(SymbolType.NftCollection);
        var createInputSymbol2Type = NFTHelper.GetCreateInputSymbolType("FJPEUKMTTO-1");
        createInputSymbol2Type.ShouldBe(SymbolType.Nft);
        var createInputSymbol3Type = NFTHelper.GetCreateInputSymbolType("aaa-1");
        createInputSymbol3Type.ShouldBe(SymbolType.Unknown);
    }

    [Fact]
    public async Task TimeStampHelperTest()
    {
        TimeStampHelper.GetTimeStampInMilliseconds().ShouldNotBeNull();
        TimeStampHelper.GetTimeStampInSeconds().ShouldNotBeNull();
        
    }
}