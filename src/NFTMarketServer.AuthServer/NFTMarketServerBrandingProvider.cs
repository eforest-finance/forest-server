using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace NFTMarketServer;

[Dependency(ReplaceServices = true)]
public class NFTMarketServerBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "NFTMarketServer";
}
