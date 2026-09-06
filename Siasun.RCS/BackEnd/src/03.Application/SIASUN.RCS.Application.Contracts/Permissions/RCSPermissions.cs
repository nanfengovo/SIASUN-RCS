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
        /// <summary>
        /// 审计日志过滤规则默认查看权限
        /// </summary>
        public const string Default = GroupName + ".AuditLogFilterRules";

        /// <summary>
        /// 创建审计日志过滤规则权限
        /// </summary>
        public const string Create = Default + ".Create";

        /// <summary>
        /// 编辑审计日志过滤规则权限
        /// </summary>
        public const string Edit = Default + ".Edit";

        /// <summary>
        /// 删除审计日志过滤规则权限
        /// </summary>
        public const string Delete = Default + ".Delete";
    }

    /// <summary>
    /// 实体变更审计规则管理权限
    /// </summary>
    public static class EntityAuditRules
    {
        /// <summary>
        /// 实体变更审计规则默认查看权限
        /// </summary>
        public const string Default = GroupName + ".EntityAuditRules";

        /// <summary>
        /// 创建实体变更审计规则权限
        /// </summary>
        public const string Create = Default + ".Create";

        /// <summary>
        /// 编辑实体变更审计规则权限
        /// </summary>
        public const string Edit = Default + ".Edit";

        /// <summary>
        /// 删除实体变更审计规则权限
        /// </summary>
        public const string Delete = Default + ".Delete";
    }

    /// <summary>
    /// 调度员操作与系统自审计日志查看与检索权限
    /// </summary>
    public static class OperationLogs
    {
        /// <summary>
        /// 操作与自审计日志默认查看检索权限
        /// </summary>
        public const string Default = GroupName + ".OperationLogs";
    }

    /// <summary>
    /// 事故排障黑匣子数据包 (.rcspack) 导出与诊断权限
    /// </summary>
    public static class FlightPack
    {
        /// <summary>
        /// 事故排障黑匣子默认访问权限
        /// </summary>
        public const string Default = GroupName + ".FlightPack";

        /// <summary>
        /// 事故排障黑匣子数据包一键打包导出权限
        /// </summary>
        public const string Export = Default + ".Export";
    }

    /// <summary>
    /// 日志级别动态调整与控制权限
    /// </summary>
    public static class LogControl
    {
        /// <summary>
        /// 动态日志控制默认访问权限
        /// </summary>
        public const string Default = GroupName + ".LogControl";

        /// <summary>
        /// 动态调整运行时日志输出级别权限
        /// </summary>
        public const string SetLevel = Default + ".SetLevel";
    }

    /// <summary>
    /// 系统运行监控与指标看板访问权限
    /// </summary>
    public static class SystemMonitor
    {
        /// <summary>
        /// 系统监控看板与指标检索权限
        /// </summary>
        public const string Default = GroupName + ".SystemMonitor";
    }

    /// <summary>
    /// 前端用户行为埋点上报权限
    /// </summary>
    public static class FrontendAudit
    {
        /// <summary>
        /// 前端行为埋点上报默认权限
        /// </summary>
        public const string Default = GroupName + ".FrontendAudit";
    }

    /// <summary>
    /// 后台定时作业监控与管理权限
    /// </summary>
    public static class BackgroundJobs
    {
        /// <summary>
        /// 后台作业监控默认访问权限
        /// </summary>
        public const string Default = GroupName + ".BackgroundJobs";

        /// <summary>
        /// 手动触发与控制后台定时作业权限
        /// </summary>
        public const string Manage = Default + ".Manage";
    }

    /// <summary>
    /// 调度员人工干预权限（取消/强制结束/指派/复位）
    /// </summary>
    public static class DispatchIntervention
    {
        /// <summary>
        /// 调度员人工干预默认访问权限
        /// </summary>
        public const string Default = GroupName + ".DispatchIntervention";

        /// <summary>
        /// 调度员人工干预取消任务权限
        /// </summary>
        public const string Cancel = Default + ".Cancel";

        /// <summary>
        /// 调度员人工强制完结任务权限
        /// </summary>
        public const string ForceEnd = Default + ".ForceEnd";

        /// <summary>
        /// 调度员人工指派特定车辆权限
        /// </summary>
        public const string Assign = Default + ".Assign";

        /// <summary>
        /// 调度员人工复位异常车辆权限
        /// </summary>
        public const string Reset = Default + ".Reset";
    }
}
