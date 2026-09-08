using System;

namespace SIASUN.RCS.Tasks.Events
{
    /// <summary>
    /// 任务步骤执行 SAGA 补偿步退领域事件
    /// </summary>
    public class TaskStepCompensatedEvent
    {
        /// <summary>
        /// 任务全局唯一主键
        /// </summary>
        public Guid TaskId { get; }

        /// <summary>
        /// 任务业务编号
        /// </summary>
        public string TaskCode { get; }

        /// <summary>
        /// 回滚发起时的步进索引
        /// </summary>
        public int FromStepIndex { get; }

        /// <summary>
        /// 补偿回退的目标步进索引
        /// </summary>
        public int TargetStepIndex { get; }

        /// <summary>
        /// 补偿原因说明
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// 全链路贯穿 TraceId
        /// </summary>
        public string? TraceId { get; }

        /// <summary>
        /// 补偿发生时间（UTC）
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskStepCompensatedEvent(
            Guid taskId,
            string taskCode,
            int fromStepIndex,
            int targetStepIndex,
            string reason,
            string? traceId = null,
            DateTime? timestamp = null)
        {
            TaskId = taskId;
            TaskCode = taskCode;
            FromStepIndex = fromStepIndex;
            TargetStepIndex = targetStepIndex;
            Reason = reason;
            TraceId = traceId;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }
    }
}
