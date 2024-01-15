using System;

namespace NFTMarketServer.Common;

public static class TimeHelper
{
    public static DateTime FromMilliSeconds(this DateTime dateTime, long milliSeconds)
    {
        return DateTime.UnixEpoch.AddMilliseconds(milliSeconds);
    }
    
    public static string ToUtcString(this DateTime dateTime)
    {
        return dateTime.ToString("o");
    }
    
    public static long ToUtcMilliSeconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }
    
    public static long ToUtcSeconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }
    
}