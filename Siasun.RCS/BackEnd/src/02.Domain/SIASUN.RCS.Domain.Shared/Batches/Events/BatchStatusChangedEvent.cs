using System;

namespace SIASUN.RCS.Batches.Events
{
    /// <summary>
    /// AGV 批次状态变迁领域事件
    /// </summary>
    public class BatchStatusChangedEvent
    {
        /// <summary>
        /// 批次实体 ID
        /// </summary>
        public Guid BatchId { get; }

        /// <summary>
        /// 批次号
        /// </summary>
        public string BatchCode { get; }

        /// <summary>
        /// 变更前旧状态
        /// </summary>
        public AgvBatchStatus OldStatus { get; }

        /// <summary>
        /// 变更后新状态
        /// </summary>
        public AgvBatchStatus NewStatus { get; }

        /// <summary>
        /// 已完成子任务数量
        /// </summary>
        public int CompletedTasksCount { get; }

        /// <summary>
        /// 总子任务数量
        /// </summary>
        public int TotalTasksCount { get; }

        /// <summary>
        /// 变迁原因
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// 全链路贯穿 TraceId
        /// </summary>
        public string? TraceId { get; }

        public BatchStatusChangedEvent(
            Guid batchId,
            string batchCode,
            AgvBatchStatus oldStatus,
            AgvBatchStatus newStatus,
            int completedTasksCount,
            int totalTasksCount,
            string? reason = null,
            string? traceId = null)
        {
            BatchId = batchId;
            BatchCode = batchCode;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            CompletedTasksCount = completedTasksCount;
            TotalTasksCount = totalTasksCount;
            Reason = reason;
            TraceId = traceId;
        }
    }
}
