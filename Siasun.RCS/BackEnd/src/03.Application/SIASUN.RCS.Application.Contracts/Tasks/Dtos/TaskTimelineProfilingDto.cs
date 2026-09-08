using System;
using System.Collections.Generic;

namespace SIASUN.RCS.Tasks.Dtos
{
    /// <summary>
    /// 任务流转时序画像与细粒度耗时画像 DTO
    /// 完整映射前端任务详情大屏弹窗：TM 状态轴、批次出库轴、系统交互流水与耗时拆解
    /// </summary>
    public class TaskTimelineProfilingDto
    {
        /// <summary>任务唯一编号</summary>
        public string TaskCode { get; set; } = string.Empty;

        /// <summary>任务粗粒度生命周期状态 (Pending/Running/Succeeded/Failed/Canceled)</summary>
        public string State { get; set; } = string.Empty;

        /// <summary>任务类型（如 Transport / Charging / Maintenance）</summary>
        public string TaskType { get; set; } = "Transport";

        /// <summary>起点站台编码</summary>
        public string? FromStation { get; set; }

        /// <summary>终点站台编码</summary>
        public string? ToStation { get; set; }

        /// <summary>载具/容器/FOUP 编号</summary>
        public string? CarrierCode { get; set; }

        /// <summary>关联批次号</summary>
        public string? BatchId { get; set; }

        /// <summary>指派执行车辆编号</summary>
        public string? AssignedVehicleCode { get; set; }

        /// <summary>任务创建时间 (UTC)</summary>
        public DateTime CreationTime { get; set; }

        /// <summary>任务开始执行时间 (UTC)</summary>
        public DateTime? StartTime { get; set; }

        /// <summary>任务完成时间 (UTC)</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>任务总执行耗时（毫秒）</summary>
        public long TotalDurationMs { get; set; }

        /// <summary>
        /// TM 状态流转轴节点列表（创建 -> 已派发 -> 取货 -> 放货 -> 等待完成）
        /// </summary>
        public List<TmStageNodeDto> TmStages { get; set; } = new();

        /// <summary>
        /// 上游出库阶段与批次计划列表（待出库 -> StockOut -> Execute -> 待放行 -> 已放行 -> 完成）
        /// </summary>
        public List<UpstreamBatchNodeDto> UpstreamBatches { get; set; } = new();

        /// <summary>
        /// 跨系统交互流水日志（AMA / Mica / PLC / LocationLock / TM），含耗时与折叠详情
        /// </summary>
        public List<SubsystemInteractionItemDto> Interactions { get; set; } = new();

        /// <summary>
        /// 任务各环节耗时拆解汇总
        /// </summary>
        public TaskMetricsBreakdownDto MetricsBreakdown { get; set; } = new();
    }

    /// <summary>
    /// TM 底层状态流转阶段节点
    /// </summary>
    public class TmStageNodeDto
    {
        /// <summary>阶段名称（创建 / 已派发 / 取货 / 放货 / 等待完成）</summary>
        public string Stage { get; set; } = string.Empty;

        /// <summary>阶段状态 (Finished / Running / Pending / Failed)</summary>
        public string State { get; set; } = "Pending";

        /// <summary>触发时间 (UTC)</summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>该阶段耗时（毫秒）</summary>
        public long DurationMs { get; set; }
    }

    /// <summary>
    /// 上游批次子计划出库流转状态
    /// </summary>
    public class UpstreamBatchNodeDto
    {
        /// <summary>子计划唯一标识</summary>
        public string PlanId { get; set; } = string.Empty;

        /// <summary>载具编号/RFID</summary>
        public string? CarrierId { get; set; }

        /// <summary>状态（成功 / 失败 / 进行中 / 待放行）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>失败原因或阶段摘要</summary>
        public string? FailedReason { get; set; }
    }

    /// <summary>
    /// 系统交互日志流水项
    /// </summary>
    public class SubsystemInteractionItemDto
    {
        /// <summary>交互发生时间戳 (UTC)</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>对接子系统 (AMA / Mica / PLC / LocationLock / TM / Arm / Vision)</summary>
        public string Subsystem { get; set; } = string.Empty;

        /// <summary>事件或接口动作名称</summary>
        public string Event { get; set; } = string.Empty;

        /// <summary>执行状态 (Success / Failed / Timeout)</summary>
        public string Status { get; set; } = "Success";

        /// <summary>该交互动作的调用耗时（毫秒）</summary>
        public long ElapsedMs { get; set; }

        /// <summary>业务摘要说明</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>详细请求/响应报文或错误详情（支持前端折叠查看）</summary>
        public string? Details { get; set; }
    }

    /// <summary>
    /// 任务核心耗时拆解度量
    /// </summary>
    public class TaskMetricsBreakdownDto
    {
        /// <summary>任务总耗时（毫秒）</summary>
        public long TotalDurationMs { get; set; }

        /// <summary>AMA 交互耗时（毫秒）</summary>
        public long AmaInteractionMs { get; set; }

        /// <summary>Mica / WMS 交互耗时（毫秒）</summary>
        public long MicaInteractionMs { get; set; }

        /// <summary>PLC 传感器与联锁交互耗时（毫秒）</summary>
        public long PlcInteractionMs { get; set; }

        /// <summary>空间库位原子锁竞争与获取耗时（毫秒）</summary>
        public long LocationLockMs { get; set; }

        /// <summary>TM 调度指令下发与握手耗时（毫秒）</summary>
        public long TmDispatchMs { get; set; }

        /// <summary>AGV 车辆纯运动导航行驶耗时（毫秒）</summary>
        public long AgvNavDurationMs { get; set; }

        /// <summary>机械臂/协作臂取放料动作耗时（毫秒）</summary>
        public long ArmActionDurationMs { get; set; }

        /// <summary>视觉二次定位与拍照耗时（毫秒）</summary>
        public long VisualAlignDurationMs { get; set; }

        /// <summary>交管避让与排队等待耗时（毫秒）</summary>
        public long TrafficWaitDurationMs { get; set; }

        /// <summary>调度内核算法与其余开销（毫秒）</summary>
        public long OtherMs { get; set; }
    }
}
