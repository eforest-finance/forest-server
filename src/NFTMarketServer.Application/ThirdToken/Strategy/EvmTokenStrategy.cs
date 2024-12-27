using System.Threading.Tasks;
using Nethereum.Web3;
using NFTMarketServer.Options;
using NFTMarketServer.ThirdToken.Index;

namespace NFTMarketServer.ThirdToken.Strategy;

public class EvmTokenStrategy : IThirdTokenStrategy
{
    public async Task<bool> CheckThirdTokenExistAsync(string tokenName, string tokenSymbol, string deployedAddress,
        string associatedTokenAccount, ThirdTokenInfo info, string abi)
    {
        var web3 = new Web3(info.Url);
        var contract = web3.Eth.GetContract(abi, info.ContractAddress);
        var doesTokenExistFunction = contract.GetFunction("doesTokenExist");
        return await doesTokenExistFunction.CallAsync<bool>(tokenName, tokenSymbol);
    }

    public ThirdTokenType GetThirdTokenType()
    {
        return ThirdTokenType.Evm;
    }
}