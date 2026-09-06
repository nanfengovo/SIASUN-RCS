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
    /// <summary>
    /// 实体变更审计日志后台批量消费者
    /// 从内存无锁通道拉取实体变更消息，进行高性能 JSON 序列化并批量持久化至存储介质，同时推流至诊断广播通道
    /// </summary>
    public class EntityAuditLogConsumer : BackgroundService
    {
        private readonly EntityAuditLogChannel _channel;
        private readonly IEntityAuditLogStore _store;
        private readonly ILogger<EntityAuditLogConsumer> _logger;
        private readonly Diagnostics.SignalR.IDiagnosticLiveStreamBroker? _liveStreamBroker;
        private readonly SIASUN.RCS.Diagnostics.IAdaptiveTrafficGovernor? _trafficGovernor;

        /// <summary>
        /// 构造函数注入所需存储、日志与限流依赖
        /// </summary>
        /// <param name="channel">内存通道</param>
        /// <param name="store">持久化存储提供者</param>
        /// <param name="logger">系统日志</param>
        /// <param name="liveStreamBroker">实时诊断流广播服务（可选）</param>
        /// <param name="trafficGovernor">自适应限流与背压降采样控制器（可选）</param>
        public EntityAuditLogConsumer(
            EntityAuditLogChannel channel,
            IEntityAuditLogStore store,
            ILogger<EntityAuditLogConsumer> logger,
            Diagnostics.SignalR.IDiagnosticLiveStreamBroker? liveStreamBroker = null,
            SIASUN.RCS.Diagnostics.IAdaptiveTrafficGovernor? trafficGovernor = null)
        {
            _channel = channel;
            _store = store;
            _logger = logger;
            _liveStreamBroker = liveStreamBroker;
            _trafficGovernor = trafficGovernor;
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
                            // L4 自适应限流保盘防护：突发洪峰时，对非核心实体的常规变更进行平滑采样
                            // 调度核心实体（AgvTask / AgvVehicle / Operator）受 IsPrivileged 保护 100% 持久化
                            if (_trafficGovernor != null)
                            {
                                var decision = _trafficGovernor.ShouldAdmit(msg.EntityName, "Information");
                                if (!decision.IsAdmitted)
                                {
                                    continue;
                                }
                            }
                            var entry = new EntityAuditLogEntry
                            {
                                TraceId = msg.TraceId,
                                EntityName = msg.EntityName,
                                EntityId = msg.EntityId,
                                Action = msg.Action,
                                CreationTime = msg.CreationTime
                            };

                            // 在后台线程执行 CPU 密集型的 JSON 序列化，并利用 Source Generator 压榨性能
                            if (msg.ChangedProperties != null && msg.OriginalValues == null && msg.CurrentValues == null)
                            {
                                // Summary Mode
                                entry.PropertyChangesJson = JsonSerializer.Serialize(msg.ChangedProperties, EntityAuditLogJsonContext.Default.ListString);
                            }
                            else
                            {
                                // Full Mode
                                var diff = new Dictionary<string, PropertyDiff>();
                                if (msg.OriginalValues != null)
                                {
                                    foreach (var kvp in msg.OriginalValues)
                                    {
                                        diff[kvp.Key] = new PropertyDiff { Old = kvp.Value, New = msg.CurrentValues?.GetValueOrDefault(kvp.Key) };
                                    }
                                }
                                else if (msg.CurrentValues != null)
                                {
                                    foreach (var kvp in msg.CurrentValues)
                                    {
                                        diff[kvp.Key] = new PropertyDiff { New = kvp.Value };
                                    }
                                }
                                entry.PropertyChangesJson = JsonSerializer.Serialize(diff, EntityAuditLogJsonContext.CombinedOptions);
                            }

                            if (_liveStreamBroker != null && _liveStreamBroker.IsEnabled)
                            {
                                _liveStreamBroker.Publish(new Diagnostics.SignalR.LiveEventDto
                                {
                                    Timestamp = entry.CreationTime,
                                    Track = "Entity",
                                    Level = "Information",
                                    Source = entry.EntityName,
                                    Title = $"[{entry.EntityName}] {entry.Action} (ID: {entry.EntityId})",
                                    Summary = entry.PropertyChangesJson,
                                    TraceId = entry.TraceId,
                                    TargetId = entry.EntityId
                                });
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

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class PropertyDiff
    {
        public object? Old { get; set; }
        public object? New { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [System.Text.Json.Serialization.JsonSerializable(typeof(List<string>))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(Dictionary<string, PropertyDiff>))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(PropertyDiff))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(string))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(int))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(long))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(bool))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(DateTime))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(Guid))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(double))]
    [System.Text.Json.Serialization.JsonSerializable(typeof(decimal))]
    internal partial class EntityAuditLogJsonContext : System.Text.Json.Serialization.JsonSerializerContext
    {
        // 组合 Source Generator 与 反射 Fallback
        // 这样既能让最外层的 Dictionary 和 List 享受 Source Generator 的极致性能，
        // 又能妥善处理 object? 中可能装箱的、未在上方显式声明的未知类型。
        public static readonly JsonSerializerOptions CombinedOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(
                Default,
                new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver())
        };
    }
}
