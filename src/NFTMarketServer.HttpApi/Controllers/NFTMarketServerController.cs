using NFTMarketServer.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers
{
    /* Inherit your controllers from this class.
     */
    public abstract class NFTMarketServerController : AbpControllerBase
    {
        protected NFTMarketServerController()
        {
            LocalizationResource = typeof(NFTMarketServerResource);
        }
    }
}