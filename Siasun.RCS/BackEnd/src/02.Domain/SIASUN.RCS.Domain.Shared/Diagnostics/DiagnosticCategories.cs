namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 诊断审计分类标准常量定义
    /// 统一限流评估、自适应采样及黑匣子定责的业务归类
    /// </summary>
    public static class DiagnosticCategories
    {
        /// <summary>
        /// 调度干预与控制分类（特权铁证）
        /// </summary>
        public const string Dispatch = "Dispatch";

        /// <summary>
        /// 调度任务生命周期分类（特权铁证）
        /// </summary>
        public const string Task = "Task";

        /// <summary>
        /// 车辆设备与底盘时序分类（特权铁证）
        /// </summary>
        public const string Vehicle = "Vehicle";

        /// <summary>
        /// 调度员人工干预操作分类（特权铁证）
        /// </summary>
        public const string Operation = "Operation";

        /// <summary>
        /// 系统自愈处置分类（特权铁证）
        /// </summary>
        public const string SelfHeal = "SelfHeal";

        /// <summary>
        /// 入站接口调用分类
        /// </summary>
        public const string InboundApi = "InboundApi";

        /// <summary>
        /// 出站接口调用分类
        /// </summary>
        public const string OutboundApi = "OutboundApi";

        /// <summary>
        /// 实体变更审计分类
        /// </summary>
        public const string EntityAudit = "EntityAudit";

        /// <summary>
        /// 遥测与心跳时序分类（高频可降采样）
        /// </summary>
        public const string Telemetry = "Telemetry";
    }
}
