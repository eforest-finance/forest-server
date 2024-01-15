using NFTMarketServer.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace NFTMarketServer.Permissions
{
    public class NFTMarketServerPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            var myGroup = context.AddGroup(NFTMarketServerPermissions.GroupName);
            //Define your own permissions here. Example:
            //myGroup.AddPermission(NFTMarketServerPermissions.MyPermission1, L("Permission:MyPermission1"));
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<NFTMarketServerResource>(name);
        }
    }
}
