namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 诊断时序事件轨道常量定义
    /// 规范定义时序分析的统一泳道，杜绝散乱魔术字符串
    /// </summary>
    public static class DiagnosticTracks
    {
        /// <summary>
        /// API 接口报文时序轨道（入站/出站接口审计）
        /// </summary>
        public const string Api = "API";

        /// <summary>
        /// 调度员与系统操作自审计轨道
        /// </summary>
        public const string Operator = "Operator";

        /// <summary>
        /// 实体关键字段变更时序轨道
        /// </summary>
        public const string Entity = "Entity";

        /// <summary>
        /// 调度任务生命周期流转时序轨道
        /// </summary>
        public const string Task = "Task";

        /// <summary>
        /// 系统事件与自愈监控轨道
        /// </summary>
        public const string System = "System";

        /// <summary>
        /// AGV 遥测与底盘时序事件轨道
        /// </summary>
        public const string Telemetry = "Telemetry";

        /// <summary>
        /// 异常与故障时序事件轨道
        /// </summary>
        public const string Exception = "Exception";

        /// <summary>
        /// AGV 车辆与硬件设备时序事件轨道
        /// </summary>
        public const string Vehicle = "Vehicle";

        /// <summary>
        /// 硬件门禁与 PLC 信号跳变时序事件轨道
        /// </summary>
        public const string HardwareGate = "HardwareGate";

        /// <summary>
        /// 任务工作流与步骤步进时序事件轨道
        /// </summary>
        public const string Workflow = "Workflow";
    }
}
