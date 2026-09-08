using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace SIASUN.RCS.Outbox
{
    /// <summary>
    /// Outbox 事务可靠消息队列默认实现
    /// </summary>
    public class OutboxQueue : IOutboxQueue
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private readonly IRepository<OutboxMessage, Guid> _repository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<OutboxQueue> _logger;

        public OutboxQueue(
            IRepository<OutboxMessage, Guid> repository,
            IGuidGenerator guidGenerator,
            ILogger<OutboxQueue> logger)
        {
            _repository = repository;
            _guidGenerator = guidGenerator;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<OutboxMessage> EnqueueAsync(
            string eventType,
            object payload,
            string? destination = null,
            string? traceId = null,
            int maxRetries = 5,
            CancellationToken cancellationToken = default)
        {
            Check.NotNullOrWhiteSpace(eventType, nameof(eventType));
            Check.NotNull(payload, nameof(payload));

            var jsonPayload = payload is string str ? str : JsonSerializer.Serialize(payload, JsonOptions);

            var message = new OutboxMessage(
                _guidGenerator.Create(),
                eventType,
                jsonPayload,
                destination,
                traceId,
                maxRetries);

            await _repository.InsertAsync(message, autoSave: false, cancellationToken: cancellationToken);

            _logger.LogInformation("事务内成功入队 Outbox 可靠消息: [Id={Id}, EventType={EventType}, Dest={Dest}, TraceId={TraceId}]",
                message.Id, message.EventType, message.Destination ?? "Default", message.TraceId ?? "None");

            return message;
        }
    }
}
