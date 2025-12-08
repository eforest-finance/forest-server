using System.Threading.Tasks;

namespace NFTMarketServer.OwnerShip.Verify;

public interface IOwnerShipVerify
{
    string IssueChain { get; }
    byte[] RecoverPublicKey(string signature, string message);
    string GetAddressByPublicKey(byte[] publicKey);
    Task<string> FetchCreatorAddressAsync(string symbol, string issueContractAddress);
}