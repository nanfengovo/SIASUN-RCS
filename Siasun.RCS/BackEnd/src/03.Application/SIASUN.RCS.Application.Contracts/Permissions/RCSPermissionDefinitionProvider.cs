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

        var locationLockPermission = myGroup.AddPermission(RCSPermissions.LocationLock.Default, L("Permission:LocationLock"));
        locationLockPermission.AddChild(RCSPermissions.LocationLock.ForceUnlock, L("Permission:LocationLock.ForceUnlock"));
        locationLockPermission.AddChild(RCSPermissions.LocationLock.Maintenance, L("Permission:LocationLock.Maintenance"));

        var locationMapPermission = myGroup.AddPermission(RCSPermissions.LocationMap.Default, L("Permission:LocationMap"));
        locationMapPermission.AddChild(RCSPermissions.LocationMap.Create, L("Permission:LocationMap.Create"));
        locationMapPermission.AddChild(RCSPermissions.LocationMap.Edit, L("Permission:LocationMap.Edit"));
        locationMapPermission.AddChild(RCSPermissions.LocationMap.Delete, L("Permission:LocationMap.Delete"));

        var locationPlcPermission = myGroup.AddPermission(RCSPermissions.LocationPlcConfig.Default, L("Permission:LocationPlcConfig"));
        locationPlcPermission.AddChild(RCSPermissions.LocationPlcConfig.Create, L("Permission:LocationPlcConfig.Create"));
        locationPlcPermission.AddChild(RCSPermissions.LocationPlcConfig.Edit, L("Permission:LocationPlcConfig.Edit"));
        locationPlcPermission.AddChild(RCSPermissions.LocationPlcConfig.Delete, L("Permission:LocationPlcConfig.Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<RCSResource>(name);
    }
}
