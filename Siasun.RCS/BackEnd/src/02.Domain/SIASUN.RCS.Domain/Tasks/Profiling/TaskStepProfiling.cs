using System;
using Volo.Abp.Domain.Entities;

namespace SIASUN.RCS.Tasks.Profiling
{
    /// <summary>
    /// 任务细粒度步骤执行剖析实体
    /// 记录调度任务流转中各子系统（AMA/Mica/PLC/TM/锁/臂）调用的起止时间、耗时与执行状态
    /// </summary>
    public class TaskStepProfiling : Entity<Guid>
    {
        /// <summary>关联的任务编号</summary>
        public string TaskCode { get; protected set; } = string.Empty;

        /// <summary>关联的批次号或子批次计划编号（可选）</summary>
        public string? BatchId { get; protected set; }

        /// <summary>关联的执行车辆编号（可选）</summary>
        public string? AgvId { get; protected set; }

        /// <summary>工作流步进索引 (StepIndex)</summary>
        public int StepIndex { get; protected set; }

        /// <summary>当前激活的程段（如 Fetch / Put / Transit）</summary>
        public string? ActiveLeg { get; protected set; }

        /// <summary>子系统名称 (AMA / Mica / PLC / LocationLock / TM / Arm / Vision / TrafficControl)</summary>
        public string Subsystem { get; protected set; } = string.Empty;

        /// <summary>具体调用的操作名称 (如 ReadSensor / LockLocation / DispatchLeg)</summary>
        public string OperationName { get; protected set; } = string.Empty;

        /// <summary>开始时间 (UTC)</summary>
        public DateTime StartTime { get; protected set; }

        /// <summary>结束时间 (UTC)</summary>
        public DateTime EndTime { get; protected set; }

        /// <summary>持续时长（毫秒）</summary>
        public long DurationMs { get; protected set; }

        /// <summary>执行状态 (Success / Failed / Timeout)</summary>
        public string Status { get; protected set; } = "Success";

        /// <summary>业务摘要说明</summary>
        public string Summary { get; protected set; } = string.Empty;

        /// <summary>执行详情快照（JSON 格式，可选）</summary>
        public string? Details { get; protected set; }

        /// <summary>全链路追踪 TraceId</summary>
        public string? TraceId { get; protected set; }

        /// <summary>ORM 反序列化保护构造函数</summary>
        protected TaskStepProfiling()
        {
        }

        /// <summary>
        /// 完整初始化步骤剖析实体
        /// </summary>
        public TaskStepProfiling(
            Guid id,
            string taskCode,
            string subsystem,
            string operationName,
            DateTime startTime,
            DateTime endTime,
            long durationMs,
            string status,
            string summary,
            string? batchId = null,
            string? agvId = null,
            int stepIndex = 0,
            string? activeLeg = null,
            string? details = null,
            string? traceId = null)
            : base(id)
        {
            TaskCode = taskCode ?? string.Empty;
            Subsystem = subsystem ?? string.Empty;
            OperationName = operationName ?? string.Empty;
            StartTime = startTime;
            EndTime = endTime;
            DurationMs = durationMs >= 0 ? durationMs : (long)(endTime - startTime).TotalMilliseconds;
            Status = string.IsNullOrWhiteSpace(status) ? "Success" : status;
            Summary = summary ?? string.Empty;
            BatchId = batchId;
            AgvId = agvId;
            StepIndex = stepIndex;
            ActiveLeg = activeLeg;
            Details = details;
            TraceId = traceId;
        }
    }
}
