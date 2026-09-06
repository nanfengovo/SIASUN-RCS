namespace SIASUN.RCS.Permissions;

/// <summary>
/// SIASUN RCS 平台系统权限定义常量树
/// </summary>
public static class RCSPermissions
{
    /// <summary>
    /// RCS 权限分组名称
    /// </summary>
    public const string GroupName = "RCS";

    /// <summary>
    /// API 报文审计日志过滤规则管理权限
    /// </summary>
    public static class AuditLogFilterRules
    {
        public const string Default = GroupName + ".AuditLogFilterRules";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    /// <summary>
    /// 实体变更审计规则管理权限
    /// </summary>
    public static class EntityAuditRules
    {
        public const string Default = GroupName + ".EntityAuditRules";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    /// <summary>
    /// 调度员操作与系统自审计日志查看与检索权限
    /// </summary>
    public static class OperationLogs
    {
        public const string Default = GroupName + ".OperationLogs";
    }

    /// <summary>
    /// 事故排障黑匣子数据包 (.rcspack) 导出与诊断权限
    /// </summary>
    public static class FlightPack
    {
        public const string Default = GroupName + ".FlightPack";
        public const string Export = Default + ".Export";
    }

    /// <summary>
    /// 日志级别动态调整与控制权限
    /// </summary>
    public static class LogControl
    {
        public const string Default = GroupName + ".LogControl";
        public const string SetLevel = Default + ".SetLevel";
    }

    /// <summary>
    /// 系统运行监控与指标看板访问权限
    /// </summary>
    public static class SystemMonitor
    {
        public const string Default = GroupName + ".SystemMonitor";
    }

    /// <summary>
    /// 前端用户行为埋点上报权限
    /// </summary>
    public static class FrontendAudit
    {
        public const string Default = GroupName + ".FrontendAudit";
    }

    /// <summary>
    /// 后台定时作业监控与管理权限
    /// </summary>
    public static class BackgroundJobs
    {
        public const string Default = GroupName + ".BackgroundJobs";
        public const string Manage = Default + ".Manage";
    }

    /// <summary>
    /// 调度员人工干预权限（取消/强制结束/指派/复位）
    /// </summary>
    public static class DispatchIntervention
    {
        public const string Default = GroupName + ".DispatchIntervention";
        public const string Cancel = Default + ".Cancel";
        public const string ForceEnd = Default + ".ForceEnd";
        public const string Assign = Default + ".Assign";
        public const string Reset = Default + ".Reset";
    }
}
