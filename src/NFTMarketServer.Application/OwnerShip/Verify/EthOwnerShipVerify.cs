using System;
using System.Text;
using System.Threading.Tasks;
using AElf;
using Microsoft.Extensions.Caching.Distributed;
using Secp256k1Net;
using NFTMarketServer.RemoteClient;
using Volo.Abp.Caching;

namespace NFTMarketServer.OwnerShip.Verify;

public class EthOwnerShipVerify : IOwnerShipVerify
{
    public string IssueChain => "ETH";
    private readonly EthClient _ethClient;
    private readonly IDistributedCache<string> _ownerShipVerifyCache;

    public EthOwnerShipVerify(EthClient ethClient, IDistributedCache<string> ownerShipVerifyCache)
    {
        _ethClient = ethClient;
        _ownerShipVerifyCache = ownerShipVerifyCache;
    }
    
    public byte[] RecoverPublicKey(string signature, string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message).ComputeHash();
        var signatureBytes = Convert.FromHexString(signature);
        var secp256K1 = new Secp256k1();
        Span<byte> publicKeyOutput = new byte[Secp256k1.PUBKEY_LENGTH];
        secp256K1.Recover(publicKeyOutput, signatureBytes, messageBytes);
        Span<byte> serializedKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
        secp256K1.PublicKeySerialize(serializedKey, publicKeyOutput);
        serializedKey = serializedKey.Slice(serializedKey.Length - Secp256k1.PUBKEY_LENGTH);
        return serializedKey.ToArray();
    }

    public string GetAddressByPublicKey(byte[] publicKey)
    {
        return "0x" + Convert.ToHexString(publicKey.ComputeHash()).Substring(24);
    }

    public async Task<string> FetchCreatorAddressAsync(string symbol, string issueContractAddress)
    {
        var cacheKey = $"{SymbolConstants.OwnerShipVerifyCachePrefix}:{IssueChain}:{symbol}";
        var ownerAddress = await _ownerShipVerifyCache.GetAsync(cacheKey);
        if (ownerAddress.IsNullOrEmpty())
        {
            ownerAddress = await _ethClient.FetchContractOwnerAsync(issueContractAddress);
            if (ownerAddress.IsNullOrEmpty())
            {
                return "";
            }
            await _ownerShipVerifyCache.SetAsync(cacheKey, ownerAddress, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(SymbolConstants.OwnerShipVerifyCacheExpireTime)
            });
        }
        return ownerAddress;
    }
}