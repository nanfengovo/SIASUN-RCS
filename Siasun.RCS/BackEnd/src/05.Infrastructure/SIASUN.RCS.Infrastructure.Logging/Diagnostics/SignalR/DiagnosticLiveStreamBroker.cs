using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using SIASUN.RCS.Diagnostics;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR
{
    /// <summary>
    /// SignalR 实时诊断与推流中台 Broker 实现
    /// 具备 LRU 主题自动淘汰、环形内存缓冲控制、特权隔离待发队列有界背压防护与动态日志级别过滤
    /// </summary>
    public class DiagnosticLiveStreamBroker : IDiagnosticLiveStreamBroker, ISingletonDependency
    {
        private static readonly HashSet<string> PermanentTopics = new(StringComparer.OrdinalIgnoreCase)
        {
            "all",
            "errors"
        };

        private readonly DiagnosticLiveStreamOptions _options;
        private readonly IAdaptiveTrafficGovernor? _trafficGovernor;
        private readonly IEvidencePrivilegePolicy _privilegePolicy;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<LiveEventDto>> _ringBuffers = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _topicLastAccessTicks = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentQueue<(string Topic, LiveEventDto Event)> _priorityPendingQueue = new();
        private readonly ConcurrentQueue<(string Topic, LiveEventDto Event)> _normalPendingQueue = new();

        /// <summary>
        /// 是否启用 SignalR 诊断推流
        /// </summary>
        public bool IsEnabled => _options.IsEnabled;

        /// <summary>
        /// 当前待发送推流队列中的事件总数（含特权与常规队列）
        /// </summary>
        public int PendingCount => _priorityPendingQueue.Count + _normalPendingQueue.Count;

        /// <summary>
        /// 构造函数注入标准配置选项、自适应限流控制器与特权策略
        /// </summary>
        /// <param name="options">规范实时推流配置选项</param>
        /// <param name="trafficGovernor">自适应限流控制器（可选）</param>
        /// <param name="privilegePolicy">特权裁决策略单一真实源（可选）</param>
        public DiagnosticLiveStreamBroker(
            IOptions<DiagnosticLiveStreamOptions>? options = null,
            IAdaptiveTrafficGovernor? trafficGovernor = null,
            IEvidencePrivilegePolicy? privilegePolicy = null)
        {
            _options = options?.Value ?? new DiagnosticLiveStreamOptions();
            _trafficGovernor = trafficGovernor;
            _privilegePolicy = privilegePolicy ?? DefaultEvidencePrivilegePolicy.Instance;
        }

        /// <summary>
        /// 兼容历史 SignalRDiagnosticsOptions 的构造函数重载
        /// </summary>
        /// <param name="signalROptions">历史 SignalR 配置选项</param>
        /// <param name="trafficGovernor">自适应限流控制器（可选）</param>
        /// <param name="privilegePolicy">特权裁决策略单一真实源（可选）</param>
        public DiagnosticLiveStreamBroker(
            IOptions<SignalRDiagnosticsOptions>? signalROptions,
            IAdaptiveTrafficGovernor? trafficGovernor = null,
            IEvidencePrivilegePolicy? privilegePolicy = null)
            : this(signalROptions != null ? Microsoft.Extensions.Options.Options.Create<DiagnosticLiveStreamOptions>(signalROptions.Value) : null, trafficGovernor, privilegePolicy)
        {
        }

        /// <summary>
        /// 发布诊断事件至对应主题频道
        /// </summary>
        /// <param name="evt">实时诊断事件 DTO</param>
        public void Publish(LiveEventDto evt)
        {
            if (!_options.IsEnabled || evt == null) return;

            // 1. L4 自适应限流背压保护：在突发洪峰或日志风暴时平滑降采样非关键遥测
            // 铁律：Warning/Error/Fatal 异常与 Operation/Task/Vehicle 铁证 100% 绝对放行
            if (_trafficGovernor != null)
            {
                var decision = _trafficGovernor.ShouldAdmit(evt.Track, evt.Level);
                if (!decision.IsAdmitted)
                {
                    return;
                }
            }

            // 2. 级别门槛过滤：低于 MinLogLevel 的事件不推流
            if (!IsLevelSatisfied(evt.Level, _options.MinLogLevel))
            {
                return;
            }

            // 1. 推送到全量主题 (all)
            AppendToTopic("all", evt);

            // 2. 如果是 Warning 或 Error 级，推送到 errors 主题
            if (string.Equals(evt.Level, DiagnosticLevels.Warning, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evt.Level, DiagnosticLevels.Error, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(evt.Level, DiagnosticLevels.Fatal, StringComparison.OrdinalIgnoreCase))
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

        /// <summary>
        /// 获取指定主题的环形缓存历史事件列表
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="maxCount">最大返回条数</param>
        /// <returns>历史事件只读列表</returns>
        public IReadOnlyList<LiveEventDto> GetHistory(string topic, int? maxCount = null)
        {
            if (!_options.IsEnabled) return Array.Empty<LiveEventDto>();

            var normalizedTopic = string.IsNullOrWhiteSpace(topic) ? "all" : topic.Trim();
            _topicLastAccessTicks[normalizedTopic] = DateTime.UtcNow.Ticks;

            if (_ringBuffers.TryGetValue(normalizedTopic, out var queue))
            {
                var limit = maxCount ?? _options.RingBufferCapacity;
                return queue.TakeLast(limit).ToList();
            }

            return Array.Empty<LiveEventDto>();
        }

        /// <summary>
        /// 批量拉取并清空待发送队列（优先清空特权事件队列，保障核心事故与调度广播绝不挤压遗漏）
        /// </summary>
        /// <returns>按主题分组的事件字典</returns>
        public Dictionary<string, List<LiveEventDto>> DequeuePendingBatches()
        {
            var batches = new Dictionary<string, List<LiveEventDto>>(StringComparer.OrdinalIgnoreCase);
            if (!_options.IsEnabled || (_priorityPendingQueue.IsEmpty && _normalPendingQueue.IsEmpty)) return batches;

            // 优先抽取特权队列
            while (_priorityPendingQueue.TryDequeue(out var item))
            {
                if (!batches.TryGetValue(item.Topic, out var list))
                {
                    list = new List<LiveEventDto>();
                    batches[item.Topic] = list;
                }
                list.Add(item.Event);
            }

            // 随后抽取常规遥测队列
            while (_normalPendingQueue.TryDequeue(out var item))
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
            // 维持 Topic 总量上限，超出时进行 LRU 淘汰
            EnsureTopicCapacity();

            _topicLastAccessTicks[topic] = DateTime.UtcNow.Ticks;
            var queue = _ringBuffers.GetOrAdd(topic, _ => new ConcurrentQueue<LiveEventDto>());
            queue.Enqueue(evt);

            // 维持单一主题环形缓冲区容量上限
            while (queue.Count > _options.RingBufferCapacity && queue.TryDequeue(out _))
            {
            }

            // 特权队列与常规队列隔离分流：
            // 核心铁证（异常、人工操作、调度关键流转）进入特权队列，绝不被常规遥测降采样挤压丢弃
            var isPrivileged = _privilegePolicy.IsPrivileged(category: evt.Source, level: evt.Level, track: evt.Track);
            if (isPrivileged)
            {
                _priorityPendingQueue.Enqueue((topic, evt));
            }
            else
            {
                // 背压防护：若常规遥测待推队列超出上限，仅丢弃最早的常规遥测积压
                while (_normalPendingQueue.Count >= _options.MaxPendingQueueSize && _normalPendingQueue.TryDequeue(out _))
                {
                }

                _normalPendingQueue.Enqueue((topic, evt));
            }
        }

        private void EnsureTopicCapacity()
        {
            if (_ringBuffers.Count <= _options.MaxActiveTopics) return;

            // 找出最久未访问且非永久主题的 key 予以淘汰
            var candidateTopics = _topicLastAccessTicks
                .Where(kvp => !PermanentTopics.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .Take(_ringBuffers.Count - _options.MaxActiveTopics + 10) // 批量淘汰，减少频繁计算
                .ToList();

            foreach (var key in candidateTopics)
            {
                _ringBuffers.TryRemove(key, out _);
                _topicLastAccessTicks.TryRemove(key, out _);
            }
        }

        private static bool IsLevelSatisfied(string? eventLevel, string minLogLevel)
        {
            var eventRank = GetLevelRank(eventLevel);
            var minRank = GetLevelRank(minLogLevel);
            return eventRank >= minRank;
        }

        private static int GetLevelRank(string? level)
        {
            if (string.IsNullOrWhiteSpace(level)) return 1; // 默认按 Information 处理

            return level.ToLowerInvariant() switch
            {
                "trace" => 0,
                "debug" => 0,
                "information" => 1,
                "info" => 1,
                "warning" => 2,
                "warn" => 2,
                "error" => 3,
                "err" => 3,
                "critical" => 4,
                "fatal" => 4,
                _ => 1
            };
        }
    }
}
