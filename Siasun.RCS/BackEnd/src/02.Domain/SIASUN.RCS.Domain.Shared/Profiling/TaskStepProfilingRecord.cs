using System;

namespace SIASUN.RCS.Profiling
{
    /// <summary>
    /// 轻量步骤剖析执行记录值对象
    /// 承载调度步骤与子系统调用的瞬时执行指标，用于异步推入缓冲队列
    /// </summary>
    public class TaskStepProfilingRecord
    {
        /// <summary>关联的调度任务唯一编号</summary>
        public string TaskCode { get; set; } = string.Empty;

        /// <summary>关联的批次号或批次子计划标识（可选）</summary>
        public string? BatchId { get; set; }

        /// <summary>关联的执行车辆编号（可选）</summary>
        public string? AgvId { get; set; }

        /// <summary>任务工作流步进索引 (StepIndex)</summary>
        public int StepIndex { get; set; }

        /// <summary>当前执行的多程段标识（如 Fetch / Put / Park）</summary>
        public string? ActiveLeg { get; set; }

        /// <summary>调用的子系统类别 (AMA / Mica / PLC / LocationLock / TM / Arm / Vision / TrafficControl)</summary>
        public string Subsystem { get; set; } = string.Empty;

        /// <summary>具体调用的操作方法或动作名称</summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>操作开始时间 (UTC)</summary>
        public DateTime StartTime { get; set; }

        /// <summary>操作结束时间 (UTC)</summary>
        public DateTime EndTime { get; set; }

        /// <summary>执行耗时（毫秒）</summary>
        public long DurationMs { get; set; }

        /// <summary>执行结果状态 (Success / Failed / Timeout)</summary>
        public string Status { get; set; } = "Success";

        /// <summary>操作业务摘要</summary>
        public string? Summary { get; set; }

        /// <summary>详细上下文或报文片段 (JSON 格式，可选)</summary>
        public string? Details { get; set; }

        /// <summary>全链路追踪 TraceId</summary>
        public string? TraceId { get; set; }
    }
}
