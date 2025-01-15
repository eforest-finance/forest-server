using System.Threading.Tasks;
using NFTMarketServer.Options;
using NFTMarketServer.ThirdToken.Index;
using NFTMarketServer.ThirdToken.Provider;

namespace NFTMarketServer.ThirdToken.Strategy;

public interface IThirdTokenStrategy
{
    Task<ThirdTokenExistDto> CheckThirdTokenExistAsync(string tokenName, string tokenSymbol, string deployedAddress,
        string associatedTokenAccount, ThirdTokenInfo info, string abi);

    ThirdTokenType GetThirdTokenType();
}