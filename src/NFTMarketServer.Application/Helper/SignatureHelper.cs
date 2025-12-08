using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Cryptography;
using Google.Protobuf;

namespace NFTMarketServer.Helper;

public static class SignatureHelper
{
    private const string Signature = "signature";

    public static string GetSignature<T>(this string privateKey, T paramObj)
    {
        return GetSignature(privateKey, ConvertObjectToSortedString(paramObj));
    }

    private static string ConvertObjectToSortedString<T>(T paramObj)
    {
        IDictionary<string, object> paramMap;
        if (paramObj is IDictionary<string, object> paramInput)
        {
            paramMap = paramInput;
        }
        else
        {
            paramMap = paramObj.GetType().GetProperties().ToDictionary(p 
                => p.Name.Substring(0,1).ToLower() + p.Name.Substring(1), p => p.GetValue(paramObj, null));
        }
        return string.Join("&", paramMap.Where(kv => kv.Key != Signature && kv.Value != null)
            .OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));
    }

    private static string GetSignature(this string privateKey, string rawData)
    {
        var privateKeyByte = ByteArrayHelper.HexStringToByteArray(privateKey);
        var dataHash = HashHelper.ComputeFrom(rawData);

        var signByte = CryptoHelper.SignWithPrivateKey(privateKeyByte, dataHash.ToByteArray());
        return ByteString.CopyFrom(signByte).ToBase64();
    }
    
    public static bool VerifySignature<T>(this string publicKey, string signature, T paramObj)
    {
       return VerifySignature(publicKey, signature, ConvertObjectToSortedString(paramObj));
    }

    private static bool VerifySignature(this string publicKey, string signature, string rawData)
    {
        var dataHash = HashHelper.ComputeFrom(rawData).ToByteArray();
        var publicKeyByte = ByteArrayHelper.HexStringToByteArray(publicKey);
        var signByte = ByteString.FromBase64(signature);
        return CryptoHelper.VerifySignature(signByte.ToByteArray(), dataHash, publicKeyByte);
    }
}