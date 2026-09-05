using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR
{
    public class DiagnosticLiveStreamBroker : IDiagnosticLiveStreamBroker, ISingletonDependency
    {
        private readonly SignalRDiagnosticsOptions _options;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<LiveEventDto>> _ringBuffers = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentQueue<(string Topic, LiveEventDto Event)> _pendingQueue = new();

        public bool IsEnabled => _options.IsEnabled;

        public DiagnosticLiveStreamBroker(IOptions<SignalRDiagnosticsOptions>? options = null)
        {
            _options = options?.Value ?? new SignalRDiagnosticsOptions();
        }

        public void Publish(LiveEventDto evt)
        {
            if (!_options.IsEnabled || evt == null) return;

            // 1. 推送到全量主题 (all)
            AppendToTopic("all", evt);

            // 2. 如果是 Warning 或 Error 级，推送到 errors 主题
            if (string.Equals(evt.Level, "Warning", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evt.Level, "Error", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evt.Level, "Fatal", StringComparison.OrdinalIgnoreCase))
            {
                AppendToTopic("errors", evt);
            }

            // 3. 如果包含 TaskId / TargetId，推送到 task:{targetId} 主题
            if (!string.IsNullOrWhiteSpace(evt.TargetId))
            {
                AppendToTopic($"task:{evt.TargetId}", evt);
            }

            // 4. 如果包含 VehicleId，推送到 vehicle:{vehicleId} 主题
            if (!string.IsNullOrWhiteSpace(evt.VehicleId))
            {
                AppendToTopic($"vehicle:{evt.VehicleId}", evt);
            }
        }

        public IReadOnlyList<LiveEventDto> GetHistory(string topic, int? maxCount = null)
        {
            if (!_options.IsEnabled) return Array.Empty<LiveEventDto>();

            var normalizedTopic = string.IsNullOrWhiteSpace(topic) ? "all" : topic.Trim();
            if (_ringBuffers.TryGetValue(normalizedTopic, out var queue))
            {
                var limit = maxCount ?? _options.RingBufferCapacity;
                return queue.TakeLast(limit).ToList();
            }

            return Array.Empty<LiveEventDto>();
        }

        public Dictionary<string, List<LiveEventDto>> DequeuePendingBatches()
        {
            var batches = new Dictionary<string, List<LiveEventDto>>(StringComparer.OrdinalIgnoreCase);
            if (!_options.IsEnabled || _pendingQueue.IsEmpty) return batches;

            while (_pendingQueue.TryDequeue(out var item))
            {
                if (!batches.TryGetValue(item.Topic, out var list))
                {
                    list = new List<LiveEventDto>();
                    batches[item.Topic] = list;
                }
                list.Add(item.Event);
            }

            return batches;
        }

        private void AppendToTopic(string topic, LiveEventDto evt)
        {
            var queue = _ringBuffers.GetOrAdd(topic, _ => new ConcurrentQueue<LiveEventDto>());
            queue.Enqueue(evt);

            // 维持环形缓冲区大小上限
            while (queue.Count > _options.RingBufferCapacity && queue.TryDequeue(out _))
            {
            }

            // 加入待批量推流队列
            _pendingQueue.Enqueue((topic, evt));
        }
    }
}
