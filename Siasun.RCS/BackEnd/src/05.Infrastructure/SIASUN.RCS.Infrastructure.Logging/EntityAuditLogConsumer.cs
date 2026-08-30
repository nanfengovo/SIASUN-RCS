using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.Logging
{
    public class EntityAuditLogConsumer : BackgroundService
    {
        private readonly EntityAuditLogChannel _channel;
        private readonly IEntityAuditLogStore _store;
        private readonly ILogger<EntityAuditLogConsumer> _logger;

        public EntityAuditLogConsumer(EntityAuditLogChannel channel, IEntityAuditLogStore store, ILogger<EntityAuditLogConsumer> logger)
        {
            _channel = channel;
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batch = new List<EntityAuditLogEntry>(100);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await _channel.Reader.WaitToReadAsync(stoppingToken))
                    {
                        while (batch.Count < 50 && _channel.Reader.TryRead(out var entry))
                        {
                            batch.Add(entry);
                        }

                        if (batch.Count > 0)
                        {
                            await _store.SaveBatchAsync(batch, stoppingToken);
                            batch.Clear();
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "异步批量持久化实体审计日志发生异常");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
