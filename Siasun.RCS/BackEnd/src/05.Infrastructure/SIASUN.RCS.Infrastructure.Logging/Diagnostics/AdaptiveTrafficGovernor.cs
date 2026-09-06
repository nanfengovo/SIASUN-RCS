using System;
using System.Threading;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 自适应限流与背压降采样控制器实现（L4 自治保护核心组件）
    /// 基于时间片滑动窗口实时统计瞬时 EPS，在突发洪峰或日志风暴时自动平滑降采样高频非关键遥测，坚决保全核心操作与异常铁证
    /// </summary>
    public class AdaptiveTrafficGovernor : IAdaptiveTrafficGovernor, ISingletonDependency
    {
        private readonly int _elevatedThresholdEps;
        private readonly int _criticalThresholdEps;
        private readonly IEvidencePrivilegePolicy _privilegePolicy;

        private long _windowStartTicks;
        private long _windowEventCount;
        private long _lastCalculatedEps;
        private long _samplingCounter;

        private long _totalAdmitted;
        private long _totalDropped;

        /// <summary>
        /// 默认构造函数，初始化典型工控机防护阈值与统一特权裁决策略
        /// </summary>
        /// <param name="elevatedThresholdEps">轻度限流阈值（每秒事件数，默认 100）</param>
        /// <param name="criticalThresholdEps">重度限流阈值（每秒事件数，默认 500）</param>
        /// <param name="privilegePolicy">统一特权裁决策略单一真实源（可选，默认 DefaultEvidencePrivilegePolicy）</param>
        public AdaptiveTrafficGovernor(
            int elevatedThresholdEps = 100,
            int criticalThresholdEps = 500,
            IEvidencePrivilegePolicy? privilegePolicy = null)
        {
            _elevatedThresholdEps = elevatedThresholdEps;
            _criticalThresholdEps = criticalThresholdEps;
            _privilegePolicy = privilegePolicy ?? DefaultEvidencePrivilegePolicy.Instance;
            _windowStartTicks = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// 判定当前事件是否准入持久化或推流广播
        /// </summary>
        /// <param name="category">事件分类（如 "Telemetry", "ApiAudit", "Operation", "EntityAudit"）</param>
        /// <param name="level">事件等级（如 "Trace", "Debug", "Info", "Warning", "Error", "Fatal"）</param>
        /// <returns>采样判定结果</returns>
        public TrafficSamplingDecision ShouldAdmit(string category, string level)
        {
            // 1. 铁证特权放行：Warning/Error/Fatal 异常与 Operation 人工干预 100% 绝对不降采样
            if (IsPrivileged(category, level))
            {
                Interlocked.Increment(ref _totalAdmitted);
                return TrafficSamplingDecision.Admit(GetCurrentLevel());
            }

            // 2. 更新滑动窗口统计与速率评估
            UpdateEpsRate();

            var currentLevel = GetCurrentLevel();
            var shouldSample = currentLevel switch
            {
                TrafficGovernorLevel.Normal => true,
                TrafficGovernorLevel.Elevated => (Interlocked.Increment(ref _samplingCounter) % 2 == 0), // 降采样 50%
                TrafficGovernorLevel.CriticalBurst => (Interlocked.Increment(ref _samplingCounter) % 10 == 0), // 降采样 90%
                _ => true
            };

            if (shouldSample)
            {
                Interlocked.Increment(ref _totalAdmitted);
                return TrafficSamplingDecision.Admit(currentLevel);
            }
            else
            {
                Interlocked.Increment(ref _totalDropped);
                var ratio = currentLevel switch
                {
                    TrafficGovernorLevel.Elevated => 0.5,
                    TrafficGovernorLevel.CriticalBurst => 0.1,
                    _ => 1.0
                };
                return TrafficSamplingDecision.Drop(currentLevel, ratio, $"自适应流量削峰抑制 ({currentLevel})");
            }
        }

        /// <summary>
        /// 获取当前限流状态指标快照
        /// </summary>
        /// <returns>包含瞬时 EPS 与累计放行/丢弃数的度量数据</returns>
        public TrafficGovernorMetrics GetMetrics()
        {
            UpdateEpsRate();

            return new TrafficGovernorMetrics
            {
                CurrentEps = Volatile.Read(ref _lastCalculatedEps),
                CurrentLevel = GetCurrentLevel(),
                TotalAdmittedCount = Volatile.Read(ref _totalAdmitted),
                TotalDroppedCount = Volatile.Read(ref _totalDropped),
                LastEvaluationTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 重置计数器与滑动时间窗口
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _windowStartTicks, DateTime.UtcNow.Ticks);
            Interlocked.Exchange(ref _windowEventCount, 0);
            Interlocked.Exchange(ref _lastCalculatedEps, 0);
            Interlocked.Exchange(ref _samplingCounter, 0);
            Interlocked.Exchange(ref _totalAdmitted, 0);
            Interlocked.Exchange(ref _totalDropped, 0);
        }

        private TrafficGovernorLevel GetCurrentLevel()
        {
            var eps = Volatile.Read(ref _lastCalculatedEps);
            if (eps >= _criticalThresholdEps)
            {
                return TrafficGovernorLevel.CriticalBurst;
            }

            if (eps >= _elevatedThresholdEps)
            {
                return TrafficGovernorLevel.Elevated;
            }

            return TrafficGovernorLevel.Normal;
        }

        private void UpdateEpsRate()
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var startTicks = Volatile.Read(ref _windowStartTicks);
            var elapsedSeconds = (nowTicks - startTicks) / (double)TimeSpan.TicksPerSecond;

            var currentCount = Interlocked.Increment(ref _windowEventCount);

            // 达到 1 秒滑动窗口，或者在短时间内事件突增达到阈值时，即刻计算刷新瞬时 EPS，实现亚秒级敏捷自适应响应
            if (elapsedSeconds >= 1.0 || currentCount >= _elevatedThresholdEps)
            {
                var count = Interlocked.Exchange(ref _windowEventCount, 0);
                Interlocked.Exchange(ref _windowStartTicks, nowTicks);
                var eps = (long)(count / Math.Max(0.001, elapsedSeconds));
                Interlocked.Exchange(ref _lastCalculatedEps, eps);
            }
        }

        private bool IsPrivileged(string category, string level)
        {
            return _privilegePolicy.IsPrivileged(category: category, level: level);
        }
    }
}
