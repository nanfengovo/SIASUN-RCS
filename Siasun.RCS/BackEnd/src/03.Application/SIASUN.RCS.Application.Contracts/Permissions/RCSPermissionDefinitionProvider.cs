using SIASUN.RCS.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace SIASUN.RCS.Permissions;

public class RCSPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(RCSPermissions.GroupName, L("Permission:RCS"));

        var filterRulesPermission = myGroup.AddPermission(RCSPermissions.AuditLogFilterRules.Default, L("Permission:AuditLogFilterRules"));
        filterRulesPermission.AddChild(RCSPermissions.AuditLogFilterRules.Create, L("Permission:Create"));
        filterRulesPermission.AddChild(RCSPermissions.AuditLogFilterRules.Edit, L("Permission:Edit"));
        filterRulesPermission.AddChild(RCSPermissions.AuditLogFilterRules.Delete, L("Permission:Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<RCSResource>(name);
    }
}
