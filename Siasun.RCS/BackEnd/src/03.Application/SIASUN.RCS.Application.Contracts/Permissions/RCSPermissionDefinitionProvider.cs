using SIASUN.RCS.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace SIASUN.RCS.Permissions;

public class RCSPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(RCSPermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(RCSPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<RCSResource>(name);
    }
}
