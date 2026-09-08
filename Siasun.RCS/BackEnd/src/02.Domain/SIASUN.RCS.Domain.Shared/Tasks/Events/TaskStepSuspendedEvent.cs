using System;

namespace SIASUN.RCS.Tasks.Events
{
    /// <summary>
    /// 任务步骤挂起等待外部异步信号领域事件
    /// </summary>
    public class TaskStepSuspendedEvent
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
        /// 当前步进索引
        /// </summary>
        public int StepIndex { get; }

        /// <summary>
        /// 所处活动程段
        /// </summary>
        public string? ActiveLeg { get; }

        /// <summary>
        /// 等待的外部事件/信号标识（如 TM 回调、PLC 门开启）
        /// </summary>
        public string WaitingEvent { get; }

        /// <summary>
        /// 全链路追踪 TraceId
        /// </summary>
        public string? TraceId { get; }

        /// <summary>
        /// 挂起发生时间（UTC）
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskStepSuspendedEvent(
            Guid taskId,
            string taskCode,
            int stepIndex,
            string? activeLeg,
            string waitingEvent,
            string? traceId = null,
            DateTime? timestamp = null)
        {
            TaskId = taskId;
            TaskCode = taskCode;
            StepIndex = stepIndex;
            ActiveLeg = activeLeg;
            WaitingEvent = waitingEvent;
            TraceId = traceId;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }
    }
}
