using System;
using System.Text;

namespace NFTMarketServer;

public static class IdGenerator
{
    private static readonly Random _random = new Random();

    public static string GenerateUniqueId()
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string timestampPart = ConvertToBase36(timestamp);

        string randomPart = GenerateRandomString(8);

        return $"{timestampPart}{randomPart}";
    }

    private static string ConvertToBase36(long value)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
        StringBuilder result = new StringBuilder();
        while (value > 0)
        {
            result.Insert(0, chars[(int)(value % 36)]);
            value /= 36;
        }
        return result.ToString();
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
        char[] result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[_random.Next(chars.Length)];
        }
        return new string(result);
    }
}