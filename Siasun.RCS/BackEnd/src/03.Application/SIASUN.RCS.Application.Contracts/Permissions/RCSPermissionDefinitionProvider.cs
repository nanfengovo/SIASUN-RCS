using SIASUN.RCS.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace SIASUN.RCS.Permissions;

/// <summary>
/// RCS 平台权限定义提供者
/// 登记权限组、权限节点及其子权限树
/// </summary>
public class RCSPermissionDefinitionProvider : PermissionDefinitionProvider
{
    /// <summary>
    /// 定义系统权限节点
    /// </summary>
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(RCSPermissions.GroupName, L("Permission:RCS"));

        var filterRulesPermission = myGroup.AddPermission(RCSPermissions.AuditLogFilterRules.Default, L("Permission:AuditLogFilterRules"));
        filterRulesPermission.AddChild(RCSPermissions.AuditLogFilterRules.Create, L("Permission:Create"));
        filterRulesPermission.AddChild(RCSPermissions.AuditLogFilterRules.Edit, L("Permission:Edit"));
        filterRulesPermission.AddChild(RCSPermissions.AuditLogFilterRules.Delete, L("Permission:Delete"));

        var entityRulesPermission = myGroup.AddPermission(RCSPermissions.EntityAuditRules.Default, L("Permission:EntityAuditRules"));
        entityRulesPermission.AddChild(RCSPermissions.EntityAuditRules.Create, L("Permission:Create"));
        entityRulesPermission.AddChild(RCSPermissions.EntityAuditRules.Edit, L("Permission:Edit"));
        entityRulesPermission.AddChild(RCSPermissions.EntityAuditRules.Delete, L("Permission:Delete"));

        myGroup.AddPermission(RCSPermissions.OperationLogs.Default, L("Permission:OperationLogs"));

        var flightPackPermission = myGroup.AddPermission(RCSPermissions.FlightPack.Default, L("Permission:FlightPack"));
        flightPackPermission.AddChild(RCSPermissions.FlightPack.Export, L("Permission:FlightPack.Export"));

        var logControlPermission = myGroup.AddPermission(RCSPermissions.LogControl.Default, L("Permission:LogControl"));
        logControlPermission.AddChild(RCSPermissions.LogControl.SetLevel, L("Permission:LogControl.SetLevel"));

        myGroup.AddPermission(RCSPermissions.SystemMonitor.Default, L("Permission:SystemMonitor"));
        myGroup.AddPermission(RCSPermissions.FrontendAudit.Default, L("Permission:FrontendAudit"));

        var backgroundJobsPermission = myGroup.AddPermission(RCSPermissions.BackgroundJobs.Default, L("Permission:BackgroundJobs"));
        backgroundJobsPermission.AddChild(RCSPermissions.BackgroundJobs.Manage, L("Permission:BackgroundJobs.Manage"));

        var dispatchPermission = myGroup.AddPermission(RCSPermissions.DispatchIntervention.Default, L("Permission:DispatchIntervention"));
        dispatchPermission.AddChild(RCSPermissions.DispatchIntervention.Cancel, L("Permission:DispatchIntervention.Cancel"));
        dispatchPermission.AddChild(RCSPermissions.DispatchIntervention.ForceEnd, L("Permission:DispatchIntervention.ForceEnd"));
        dispatchPermission.AddChild(RCSPermissions.DispatchIntervention.Assign, L("Permission:DispatchIntervention.Assign"));
        dispatchPermission.AddChild(RCSPermissions.DispatchIntervention.Reset, L("Permission:DispatchIntervention.Reset"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<RCSResource>(name);
    }
}
