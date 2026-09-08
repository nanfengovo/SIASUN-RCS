using System;

namespace SIASUN.RCS.Tasks.Events
{
    /// <summary>
    /// 任务从失败状态恢复（Resume）领域事件
    /// </summary>
    public class TaskLifecycleResumedEvent
    {
        /// <summary>
        /// 任务全局唯一主键
        /// </summary>
        public Guid TaskId { get; }

        /// <summary>
        /// 任务编号
        /// </summary>
        public string TaskCode { get; }

        /// <summary>
        /// 恢复原因说明（人工调度指令或自愈触发）
        /// </summary>
        public string ResumeReason { get; }

        /// <summary>
        /// 恢复时的步进索引
        /// </summary>
        public int StepIndex { get; }

        /// <summary>
        /// 全链路贯穿 TraceId
        /// </summary>
        public string? TraceId { get; }

        /// <summary>
        /// 恢复时间（UTC）
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskLifecycleResumedEvent(
            Guid taskId,
            string taskCode,
            string resumeReason,
            int stepIndex,
            string? traceId = null,
            DateTime? timestamp = null)
        {
            TaskId = taskId;
            TaskCode = taskCode;
            ResumeReason = resumeReason;
            StepIndex = stepIndex;
            TraceId = traceId;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }
    }
}
