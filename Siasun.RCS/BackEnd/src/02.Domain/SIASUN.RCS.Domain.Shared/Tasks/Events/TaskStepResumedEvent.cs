using System;

namespace SIASUN.RCS.Tasks.Events
{
    /// <summary>
    /// 任务挂起步进被外部信号成功唤醒领域事件
    /// </summary>
    public class TaskStepResumedEvent
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
        /// 触发唤醒的外部事件标识
        /// </summary>
        public string ReceivedEvent { get; }

        /// <summary>
        /// 全链路追踪 TraceId
        /// </summary>
        public string? TraceId { get; }

        /// <summary>
        /// 唤醒发生时间（UTC）
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskStepResumedEvent(
            Guid taskId,
            string taskCode,
            int stepIndex,
            string receivedEvent,
            string? traceId = null,
            DateTime? timestamp = null)
        {
            TaskId = taskId;
            TaskCode = taskCode;
            StepIndex = stepIndex;
            ReceivedEvent = receivedEvent;
            TraceId = traceId;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }
    }
}
