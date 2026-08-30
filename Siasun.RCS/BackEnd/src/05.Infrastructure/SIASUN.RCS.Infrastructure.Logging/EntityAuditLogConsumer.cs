using System;
using System.Collections.Generic;
using System.Text.Json;
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
                        while (batch.Count < 50 && _channel.Reader.TryRead(out var msg))
                        {
                            var entry = new EntityAuditLogEntry
                            {
                                TraceId = msg.TraceId,
                                EntityName = msg.EntityName,
                                EntityId = msg.EntityId,
                                Action = msg.Action,
                                CreationTime = msg.CreationTime
                            };

                            // 在后台线程执行 CPU 密集型的 JSON 序列化
                            if (msg.ChangedProperties != null && msg.OriginalValues == null && msg.CurrentValues == null)
                            {
                                // Summary Mode
                                entry.PropertyChangesJson = JsonSerializer.Serialize(msg.ChangedProperties);
                            }
                            else
                            {
                                // Full Mode
                                var diff = new Dictionary<string, object?>();
                                if (msg.OriginalValues != null)
                                {
                                    foreach (var kvp in msg.OriginalValues)
                                    {
                                        diff[kvp.Key] = new { Old = kvp.Value, New = msg.CurrentValues?.GetValueOrDefault(kvp.Key) };
                                    }
                                }
                                else if (msg.CurrentValues != null)
                                {
                                    foreach (var kvp in msg.CurrentValues)
                                    {
                                        diff[kvp.Key] = new { New = kvp.Value };
                                    }
                                }
                                entry.PropertyChangesJson = JsonSerializer.Serialize(diff);
                            }

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
                    batch.Clear(); // 防死循环
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
