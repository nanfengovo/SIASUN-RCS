using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Outbox
{
    /// <summary>
    /// Outbox 事务可靠消息聚合根（保证跨领域与外部系统通知的最终一致性与断电防丢）
    /// </summary>
    public class OutboxMessage : FullAuditedAggregateRoot<Guid>
    {
        /// <summary>
        /// 领域事件或消息类型标识（如 "TaskCompleted", "SagaCompensationRequired"）
        /// </summary>
        public string EventType { get; private set; } = string.Empty;

        /// <summary>
        /// 消息 JSON 报文载荷
        /// </summary>
        public string Payload { get; private set; } = string.Empty;

        /// <summary>
        /// 投递状态
        /// </summary>
        public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;

        /// <summary>
        /// 已重试次数
        /// </summary>
        public int RetryCount { get; private set; }

        /// <summary>
        /// 最大允许重试次数（默认 5 次）
        /// </summary>
        public int MaxRetries { get; private set; } = 5;

        /// <summary>
        /// 下一次允许重试的时间戳（UTC）
        /// </summary>
        public DateTime? NextRetryTime { get; private set; }

        /// <summary>
        /// 目标下游系统（如 "MES", "AMA", "WMS"）
        /// </summary>
        public string? Destination { get; private set; }

        /// <summary>
        /// 全链路贯穿 TraceId
        /// </summary>
        public string? TraceId { get; private set; }

        /// <summary>
        /// 最近一次重试失败错误信息
        /// </summary>
        public string? LastError { get; private set; }

        /// <summary>
        /// 消息最终处理成功或转死信的时间（UTC）
        /// </summary>
        public DateTime? ProcessedTime { get; private set; }

        /// <summary>
        /// EF Core 反序列化构造函数
        /// </summary>
        protected OutboxMessage()
        {
        }

        /// <summary>
        /// 创建新的待发布可靠消息
        /// </summary>
        public OutboxMessage(
            Guid id,
            string eventType,
            string payload,
            string? destination = null,
            string? traceId = null,
            int maxRetries = 5) : base(id)
        {
            EventType = Check.NotNullOrWhiteSpace(eventType, nameof(eventType));
            Payload = Check.NotNullOrWhiteSpace(payload, nameof(payload));
            Destination = destination;
            TraceId = traceId;
            MaxRetries = maxRetries;
            Status = OutboxMessageStatus.Pending;
            RetryCount = 0;
            NextRetryTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 标记为正在投递
        /// </summary>
        public void MarkAsPublishing()
        {
            Status = OutboxMessageStatus.Publishing;
        }

        /// <summary>
        /// 标记为已成功投递
        /// </summary>
        public void MarkAsPublished()
        {
            Status = OutboxMessageStatus.Published;
            ProcessedTime = DateTime.UtcNow;
            LastError = null;
        }

        /// <summary>
        /// 记录重试失败并按照指数退避策略计算下次重试时间
        /// </summary>
        /// <param name="error">错误原因</param>
        /// <param name="retryDelay">退避延迟时间</param>
        public void RecordRetryFailure(string error, TimeSpan retryDelay)
        {
            RetryCount++;
            LastError = Check.NotNullOrWhiteSpace(error, nameof(error));

            if (RetryCount >= MaxRetries)
            {
                MarkAsDeadLetter($"已达最大重试上限 ({MaxRetries} 次): {error}");
            }
            else
            {
                Status = OutboxMessageStatus.Publishing;
                NextRetryTime = DateTime.UtcNow.Add(retryDelay);
            }
        }

        /// <summary>
        /// 标记为死信（需人工干预排查）
        /// </summary>
        /// <param name="reason">死信原因</param>
        public void MarkAsDeadLetter(string reason)
        {
            Status = OutboxMessageStatus.DeadLetter;
            LastError = reason;
            ProcessedTime = DateTime.UtcNow;
        }
    }
}
