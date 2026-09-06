using System;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 自适应限流背压保护等级
    /// </summary>
    public enum TrafficGovernorLevel
    {
        /// <summary>
        /// 常规状态（全量采集，不降采样）
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 流量中度偏高（对低优先级常规遥测适度降采样 50%）
        /// </summary>
        Elevated = 1,

        /// <summary>
        /// 突发洪峰/雪崩状态（对高频重复遥测强力降采样 90%，坚决保护审计与异常铁证）
        /// </summary>
        CriticalBurst = 2
    }

    /// <summary>
    /// 自适应限流采样判定结果
    /// </summary>
    public class TrafficSamplingDecision
    {
        /// <summary>
        /// 是否允许本次事件通过记录或广播
        /// </summary>
        public bool IsAdmitted { get; set; }

        /// <summary>
        /// 当前系统处于的限流负载等级
        /// </summary>
        public TrafficGovernorLevel CurrentLevel { get; set; }

        /// <summary>
        /// 当前生效的采样比率 (0.0 ~ 1.0)
        /// </summary>
        public double SamplingRatio { get; set; } = 1.0;

        /// <summary>
        /// 丢弃或降采样原因说明
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 便捷构建准许决策
        /// </summary>
        public static TrafficSamplingDecision Admit(TrafficGovernorLevel level = TrafficGovernorLevel.Normal, double ratio = 1.0)
        {
            return new TrafficSamplingDecision
            {
                IsAdmitted = true,
                CurrentLevel = level,
                SamplingRatio = ratio
            };
        }

        /// <summary>
        /// 便捷构建降采样丢弃决策
        /// </summary>
        public static TrafficSamplingDecision Drop(TrafficGovernorLevel level, double ratio, string reason)
        {
            return new TrafficSamplingDecision
            {
                IsAdmitted = false,
                CurrentLevel = level,
                SamplingRatio = ratio,
                Reason = reason
            };
        }
    }

    /// <summary>
    /// 自适应流量控制器运行时指标快照
    /// </summary>
    public class TrafficGovernorMetrics
    {
        /// <summary>
        /// 当前滑动窗口瞬时速率 (每秒事件数 / EPS)
        /// </summary>
        public double CurrentEps { get; set; }

        /// <summary>
        /// 当前限流保护等级
        /// </summary>
        public TrafficGovernorLevel CurrentLevel { get; set; }

        /// <summary>
        /// 累计准入事件总数
        /// </summary>
        public long TotalAdmittedCount { get; set; }

        /// <summary>
        /// 累计降采样过滤事件总数
        /// </summary>
        public long TotalDroppedCount { get; set; }

        /// <summary>
        /// 最近一次评估时间
        /// </summary>
        public DateTime LastEvaluationTime { get; set; } = DateTime.UtcNow;
    }
}
