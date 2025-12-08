using Shouldly;
using Xunit;

namespace NFTMarketServer.NFT;

public class SymbolHelperTests
{
    [Fact]
    public async void SymbolHelperTest()
    {
        SymbolHelper.CoinGeckoELF().ShouldBe("ELF");
        SymbolHelper.MainChainSymbol().ShouldBe("AELF");
        SymbolHelper.SubHyphenNumber("aaa-1").ShouldBe(1);
        SymbolHelper.GetTrailingNumber("aaa-111").ShouldBe("111");
        SymbolHelper.GetTrailingNumber("").ShouldBe("");
        SymbolHelper.GetTrailingNumber("aaa").ShouldBe("");
        SymbolHelper.GetTrailingNumber("aaa-").ShouldBe("");
    }
}