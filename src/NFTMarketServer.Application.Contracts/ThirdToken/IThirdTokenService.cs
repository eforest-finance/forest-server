using System.Collections.Generic;
using System.Threading.Tasks;

namespace NFTMarketServer.ThirdToken;

public interface IThirdTokenService
{
    Task<MyThirdTokenResult> GetMyThirdTokenListAsync(GetMyThirdTokenInput input);
    Task<ThirdTokenPrepareBindingDto> ThirdTokenPrepareBindingAsync(ThirdTokenPrepareBindingInput input);
    Task<string> ThirdTokenBindingAsync(ThirdTokenBindingInput input);
}