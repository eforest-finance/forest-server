using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NFTMarketServer;

public class RegexHelper
{
    private static readonly Dictionary<RegexType, string> RegexPatternMap = new()
    {
        { RegexType.Email, @"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$" },
        { RegexType.Twitter, @"https?://(?:www\.)?twitter\.com/(?:\w+/status/)?\w+" },
        { RegexType.Instagram, @"^(https?:\/\/)?(www\.)?instagram\.com\/[a-zA-Z0-9-_]+\/?$" },
        { RegexType.HttpAddress, @"((https?|ftp):\/\/)?([a-z0-9-]+\.)+[a-z]{2,}(:\d{1,5})?(\/[^\s]*)?" },
        { RegexType.UserName, @"^[A-Za-z0-9]+$" }
    };

    public static bool IsValid(string str, RegexType type)
    {
        if (string.IsNullOrEmpty(str))
        {
            return true;
        }

        string pattern = RegexPatternMap[type];
        return Regex.IsMatch(str, pattern);
    }
}

public enum RegexType
{
    Email,
    Twitter,
    Instagram,
    HttpAddress,
    UserName
}