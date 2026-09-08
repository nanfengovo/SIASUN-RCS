using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Outbox;

namespace SIASUN.RCS.Infrastructure.BackgroundJobs.Outbox
{
    /// <summary>
    /// Outbox 消息分发投递默认实现（模拟/测试与通用 HTTP/MQTT 兜底）
    /// </summary>
    public class MockOutboxMessageDispatcher : IOutboxMessageDispatcher
    {
        private readonly ILogger<MockOutboxMessageDispatcher> _logger;

        public MockOutboxMessageDispatcher(ILogger<MockOutboxMessageDispatcher> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("OutboxDispatcher: 成功将可靠消息投递给目标系统 [{Dest}]: [EventType={EventType}, TraceId={TraceId}, MessageId={Id}]",
                message.Destination ?? "Default", message.EventType, message.TraceId ?? "None", message.Id);

            return Task.CompletedTask;
        }
    }
}
