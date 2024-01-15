using System;
using Shouldly;
using Xunit;

namespace NFTMarketServer.NFT;

public class DateTimeHelperTest
{
    [Fact]
    public async void TestDateTimeHelper()
    {
        DateTimeHelper.FromUnixTimeMilliseconds(1691719617);
        DateTimeHelper.ToUnixTimeMilliseconds(new DateTime());
        DateTimeHelper.AddHours(new DateTime(),1);
    }
}