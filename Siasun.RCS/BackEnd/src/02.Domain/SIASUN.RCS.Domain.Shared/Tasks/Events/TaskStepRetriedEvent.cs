using System;

namespace SIASUN.RCS.Tasks.Events
{
    /// <summary>
    /// 任务步骤执行重试领域事件（用于贯穿 TraceId 写入审计与 FlightPack 步进诊断轨）
    /// </summary>
    public class TaskStepRetriedEvent
    {
        /// <summary>
        /// 任务全局唯一标识
        /// </summary>
        public Guid TaskId { get; }

        /// <summary>
        /// 任务业务编号
        /// </summary>
        public string TaskCode { get; }

        /// <summary>
        /// 当前步进索引
        /// </summary>
        public int StepIndex { get; }

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string StepName { get; }

        /// <summary>
        /// 当前已重试次数
        /// </summary>
        public int RetryCount { get; }

        /// <summary>
        /// 最大允许重试次数
        /// </summary>
        public int MaxRetryCount { get; }

        /// <summary>
        /// 是否为幂等步进
        /// </summary>
        public bool IsIdempotent { get; }

        /// <summary>
        /// 导致重试的错误原因
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// 全链路贯穿 TraceId
        /// </summary>
        public string? TraceId { get; }

        /// <summary>
        /// 重试发生时间（UTC）
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskStepRetriedEvent(
            Guid taskId,
            string taskCode,
            int stepIndex,
            string stepName,
            int retryCount,
            int maxRetryCount,
            bool isIdempotent,
            string errorMessage,
            string? traceId = null,
            DateTime? timestamp = null)
        {
            TaskId = taskId;
            TaskCode = taskCode;
            StepIndex = stepIndex;
            StepName = stepName;
            RetryCount = retryCount;
            MaxRetryCount = maxRetryCount;
            IsIdempotent = isIdempotent;
            ErrorMessage = errorMessage;
            TraceId = traceId;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }
    }
}
