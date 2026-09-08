using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Outbox;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.BackgroundJobs.Outbox
{
    /// <summary>
    /// Outbox 消息外部系统分发投递契约
    /// </summary>
    public interface IOutboxMessageDispatcher : ITransientDependency
    {
        /// <summary>
        /// 将可靠消息投递给外部下游系统（MES / AMA / WMS）
        /// </summary>
        Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    }
}
