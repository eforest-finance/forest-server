using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;

namespace NFTMarketServer.Dealer.Helper;

public class SenderAccount
{
    private readonly ECKeyPair _keyPair;
    public Address Address { get; set; }
    
    public SenderAccount(string privateKey)
    {
        var pk = ByteArrayHelper.HexStringToByteArray(privateKey);
        _keyPair = CryptoHelper.FromPrivateKey(pk);
        Address = Address.FromPublicKey(_keyPair.PublicKey);
    }
    
    public ByteString GetSignatureWith(byte[] txData)
    {
        var signature = CryptoHelper.SignWithPrivateKey(_keyPair.PrivateKey, txData);
        return ByteString.CopyFrom(signature);
    }

}