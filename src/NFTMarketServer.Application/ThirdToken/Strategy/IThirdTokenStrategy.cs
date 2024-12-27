using System.Threading.Tasks;
using NFTMarketServer.Options;
using NFTMarketServer.ThirdToken.Index;

namespace NFTMarketServer.ThirdToken.Strategy;

public interface IThirdTokenStrategy
{
    Task<bool> CheckThirdTokenExistAsync(string tokenName, string tokenSymbol, string deployedAddress,
        string associatedTokenAccount, ThirdTokenInfo info, string abi);

    ThirdTokenType GetThirdTokenType();
}