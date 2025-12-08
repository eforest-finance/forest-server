using FluentAssertions;
using Nest;
using Xunit;

namespace NFTMarketServer;

public class IdGenerateHelperTests
{
    [Fact]
    public async void IdGenerateHelperTest()
    {
        IdGenerateHelper.GetId();
        IdGenerateHelper.GetNftActivityId("a","b","c","d","e");
        IdGenerateHelper.GetTokenInfoId("a","b");
        IdGenerateHelper.GetUserBalanceId("a","b","c");
        IdGenerateHelper.GetNFTCollectionId("a","b");
        IdGenerateHelper.GetListingWhitelistPriceId("a","b");
        IdGenerateHelper.GetNFTInfoId("a","b");
    }
}