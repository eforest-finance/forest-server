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
    
    public static long GetNumberFromStr(string numberString)
    {
        return long.Parse(numberString);
    }
    
    public static string GetDateTimeFormatted(DateTime dateTime)
    {
        return dateTime.ToString("yyyyMMddHH");
    }
    
    public static string GetUnixTimestampSecondsFormatted(long unixTimestamp)
    {
        return GetDateTimeFormatted(FromUnixTimestampSeconds(unixTimestamp));
    }
    
    public static DateTime FromUnixTimestampSeconds(long unixTimestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).ToLocalTime().DateTime;
    }
    
    public static DateTime FromUnixTimestampMilliseconds(long unixTimestamp)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).ToLocalTime().DateTime;
    }
    
    public static long GetUtcHourStartTimestamp()
    {
        var currentUtcTime = DateTime.UtcNow;
        var hourStart = new DateTime(currentUtcTime.Year, currentUtcTime.Month, currentUtcTime.Day, currentUtcTime.Hour, 0, 0, DateTimeKind.Utc);
        return (long)(hourStart - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    public static DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }
    
    public static long GetPreUtcHourStartTimestamp(long unixTimestampSeconds)
    {
        return GetBeforeUtcHourStartTimestamp(unixTimestampSeconds,1);
    }

    public static long GetPreDayUtcHourStartTimestamp(long unixTimestampSeconds)
    {
        return GetBeforeUtcHourStartTimestamp(unixTimestampSeconds, 24);
    }

    public static long GetPreWeekUtcHourStartTimestamp(long unixTimestampSeconds)
    {
        return GetBeforeUtcHourStartTimestamp(unixTimestampSeconds, 24 * 7);
    }

    public static long GetNextUtcHourStartTimestamp(long unixTimestampSeconds)
    {
        return GetAfterUtcHourStartTimestamp(unixTimestampSeconds, 1);
    }

    public static long GetAfterUtcHourStartTimestamp(long unixTimestampSeconds, int intervalNumber)
    {
        return unixTimestampSeconds + 60 * 60 * intervalNumber;
    }
    
    public static long GetBeforeUtcHourStartTimestamp(long unixTimestampSeconds, int intervalNumber)
    {
        return unixTimestampSeconds - 60 * 60 * intervalNumber;
    }
    
    public static bool IsWithin30MinutesUtc(DateTime utcTargetTime)
    {
        var currentUtcTime = DateTime.UtcNow;
        
        var timeSpan = currentUtcTime - utcTargetTime;
        var minutesDifference = Math.Abs(timeSpan.TotalMinutes);
        
        return minutesDifference <= 30;
    }


}