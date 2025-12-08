using System.Text.RegularExpressions;

namespace NFTMarketServer.Common;

public static class ValidateHelper
{
    public static bool IsContainsSpecialCharacter(string str)
    {
        string pattern = @"[~`!@#$%^&*()-+=\[\]{}|;:'""<>,.?/\\]";
        Match match = Regex.Match(str, pattern);
        if (match.Success)
        {
            return true;
        }

        return false;
    }

    public static bool IsContainsNumbers(string str)
    {
        string pattern = @"^[^\d]+$";
        Match match = Regex.Match(str, pattern);
        if (match.Success)
        {
            return false;
        }

        return true;
    }

    public static bool IsEmail(string address)
    {
        var emailRegex = @"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
        var emailReg = new Regex(emailRegex);
        return emailReg.IsMatch(address.Trim());
    }

    public static bool IsPhone(string phoneNumber)
    {
        var phoneRegex = @"^1[0-9]{10}$";
        var emailReg = new Regex(phoneRegex);
        return emailReg.IsMatch(phoneNumber.Trim());
    }
}