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

        private long _windowStartTicks;
        private long _windowEventCount;
        private long _lastCalculatedEps;
        private long _samplingCounter;

        private long _totalAdmitted;
        private long _totalDropped;

        /// <summary>
        /// 默认构造函数，初始化典型工控机防护阈值
        /// </summary>
        /// <param name="elevatedThresholdEps">轻度限流阈值（每秒事件数，默认 100）</param>
        /// <param name="criticalThresholdEps">重度限流阈值（每秒事件数，默认 500）</param>
        public AdaptiveTrafficGovernor(int elevatedThresholdEps = 100, int criticalThresholdEps = 500)
        {
            _elevatedThresholdEps = elevatedThresholdEps;
            _criticalThresholdEps = criticalThresholdEps;
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
            if (currentLevel == TrafficGovernorLevel.Normal)
            {
                Interlocked.Increment(ref _totalAdmitted);
                return TrafficSamplingDecision.Admit(currentLevel, 1.0);
            }

            // 3. 根据当前压力等级动态降采样常规高频事件
            var counter = Interlocked.Increment(ref _samplingCounter);
            if (currentLevel == TrafficGovernorLevel.Elevated)
            {
                // 50% 采样比率
                if (counter % 2 == 0)
                {
                    Interlocked.Increment(ref _totalAdmitted);
                    return TrafficSamplingDecision.Admit(currentLevel, 0.5);
                }

                Interlocked.Increment(ref _totalDropped);
                return TrafficSamplingDecision.Drop(currentLevel, 0.5, "流量偏高触发自适应 50% 降采样保护");
            }

            // CriticalBurst 突发洪峰：10% 采样比率
            if (counter % 10 == 0)
            {
                Interlocked.Increment(ref _totalAdmitted);
                return TrafficSamplingDecision.Admit(currentLevel, 0.1);
            }

            Interlocked.Increment(ref _totalDropped);
            return TrafficSamplingDecision.Drop(currentLevel, 0.1, "突发洪峰触发自适应 90% 降采样保盘保护");
        }

        /// <summary>
        /// 获取当前自适应限流状态指标快照
        /// </summary>
        /// <returns>运行时指标快照</returns>
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
        /// 重置统计计数器（便于现场诊断或单元测试）
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
            if (eps >= _criticalThresholdEps) return TrafficGovernorLevel.CriticalBurst;
            if (eps >= _elevatedThresholdEps) return TrafficGovernorLevel.Elevated;
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

        private static bool IsPrivileged(string category, string level)
        {
            if (string.Equals(level, DiagnosticLevels.Warning, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(level, DiagnosticLevels.Error, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(level, DiagnosticLevels.Fatal, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(level, "Critical", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(category, DiagnosticCategories.Operation, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, DiagnosticTracks.Operator, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, DiagnosticCategories.SelfHeal, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, DiagnosticCategories.Dispatch, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, DiagnosticCategories.Task, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "AgvTask", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Task", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, DiagnosticCategories.Vehicle, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "AgvVehicle", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Vehicle", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "TM", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "MES", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Exception", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
