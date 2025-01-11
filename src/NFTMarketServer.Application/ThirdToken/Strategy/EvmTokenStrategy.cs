using System.Threading.Tasks;
using Nethereum.Web3;
using NFTMarketServer.Options;
using NFTMarketServer.ThirdToken.Index;
using NFTMarketServer.ThirdToken.Provider;

namespace NFTMarketServer.ThirdToken.Strategy;

public class EvmTokenStrategy : IThirdTokenStrategy
{
    public async Task<ThirdTokenExistDto> CheckThirdTokenExistAsync(string tokenName, string tokenSymbol, string deployedAddress,
        string associatedTokenAccount, ThirdTokenInfo info, string abi)
    {
        var web3 = new Web3(info.Url);
        var contract = web3.Eth.GetContract(abi, info.ContractAddress);
        var doesTokenExistFunction = contract.GetFunction("getTokenAddress");

        var result = await doesTokenExistFunction.CallAsync<string>(tokenName, tokenSymbol);
        return new ThirdTokenExistDto()
        {
            Exist = result != null,
            TokenContractAddress = result
        };
    }

    public ThirdTokenType GetThirdTokenType()
    {
        return ThirdTokenType.Evm;
    }
}