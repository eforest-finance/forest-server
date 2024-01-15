using NFTMarketServer.Localization;
using Volo.Abp.Application.Services;

namespace NFTMarketServer
{
    /* Inherit your application services from this class.
     */
    public abstract class NFTMarketServerAppService : ApplicationService
    {
        protected NFTMarketServerAppService()
        {
            LocalizationResource = typeof(NFTMarketServerResource);
        }
    }
}
