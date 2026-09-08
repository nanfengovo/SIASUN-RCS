using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Outbox
{
    /// <summary>
    /// Outbox 事务可靠消息排队写入契约
    /// 在当前业务事务（Unit of Work）内将待发消息作为同一事务写入物理表，达成 100% 事务原子性
    /// </summary>
    public interface IOutboxQueue : ITransientDependency
    {
        /// <summary>
        /// 将待发布集成消息写入本地 Outbox 表
        /// </summary>
        /// <param name="eventType">事件类型代号</param>
        /// <param name="payload">强类型或匿名载荷对象（内部自动序列化为 JSON）</param>
        /// <param name="destination">目标接收系统（如 "MES"、"AMA"、"WMS"）</param>
        /// <param name="traceId">贯穿全链路 TraceId</param>
        /// <param name="maxRetries">允许的最大重试次数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>持久化创建的 OutboxMessage 实体</returns>
        Task<OutboxMessage> EnqueueAsync(
            string eventType,
            object payload,
            string? destination = null,
            string? traceId = null,
            int maxRetries = 5,
            CancellationToken cancellationToken = default);
    }
}
