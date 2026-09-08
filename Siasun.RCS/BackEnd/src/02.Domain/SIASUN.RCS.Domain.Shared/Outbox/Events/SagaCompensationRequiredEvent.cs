using System;

namespace SIASUN.RCS.Outbox.Events
{
    /// <summary>
    /// SAGA 业务补偿触发领域事件（步退回滚或非幂等严重失败时广播）
    /// </summary>
    public class SagaCompensationRequiredEvent
    {
        /// <summary>
        /// 任务实体 ID
        /// </summary>
        public Guid TaskId { get; }

        /// <summary>
        /// 任务编号
        /// </summary>
        public string TaskCode { get; }

        /// <summary>
        /// 触发补偿的故障步骤索引
        /// </summary>
        public int FailedStepIndex { get; }

        /// <summary>
        /// 故障根因描述
        /// </summary>
        public string FailureReason { get; }

        /// <summary>
        /// 拟执行的补偿动作类型（如 "RollbackWmsBooking", "ReleaseSensorGate", "RetractGripper"）
        /// </summary>
        public string CompensationAction { get; }

        /// <summary>
        /// 全链路贯穿 TraceId
        /// </summary>
        public string? TraceId { get; }

        /// <summary>
        /// 发生时间
        /// </summary>
        public DateTime Timestamp { get; }

        public SagaCompensationRequiredEvent(
            Guid taskId,
            string taskCode,
            int failedStepIndex,
            string failureReason,
            string compensationAction,
            string? traceId = null,
            DateTime? timestamp = null)
        {
            TaskId = taskId;
            TaskCode = taskCode;
            FailedStepIndex = failedStepIndex;
            FailureReason = failureReason;
            CompensationAction = compensationAction;
            TraceId = traceId;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }
    }
}
