using System;
using System.Collections.Generic;

namespace SIASUN.RCS.Tasks.Dtos
{
    /// <summary>
    /// 任务执行耗时统计与 Dashboard 报表指标 DTO
    /// 全面支撑 Dashboard_数据统计(1).xlsx 中的 P0 性能指标统计需求
    /// </summary>
    public class TaskExecutionMetricsSummaryDto
    {
        /// <summary>统计时间区间起点</summary>
        public DateTime StartTime { get; set; }

        /// <summary>统计时间区间终点</summary>
        public DateTime EndTime { get; set; }

        /// <summary>指定区域或车体过滤（可选）</summary>
        public string? FilterArea { get; set; }

        /// <summary>总任务数</summary>
        public int TotalTasks { get; set; }

        /// <summary>成功完成任务数</summary>
        public int SucceededTasks { get; set; }

        /// <summary>失败任务数</summary>
        public int FailedTasks { get; set; }

        /// <summary>
        /// P0: 单个任务完成时长统计（含平均、上极限 Max、下极限 Min）
        /// </summary>
        public DurationMetricDto TotalTaskDuration { get; set; } = new();

        /// <summary>
        /// P0: 机械臂动作时长统计（含平均、上极限 Max、下极限 Min）
        /// </summary>
        public DurationMetricDto ArmActionDuration { get; set; } = new();

        /// <summary>
        /// P0: AGV 运动时长统计（含平均、上极限 Max、下极限 Min）
        /// </summary>
        public DurationMetricDto AgvMovementDuration { get; set; } = new();

        /// <summary>
        /// P0: 二次定位含拍照时长统计（含平均、上极限 Max、下极限 Min）
        /// </summary>
        public DurationMetricDto VisualAlignDuration { get; set; } = new();

        /// <summary>
        /// P0: 相互交管等待时间统计（含平均、上极限 Max、下极限 Min）
        /// </summary>
        public DurationMetricDto TrafficWaitDuration { get; set; } = new();

        /// <summary>
        /// 各子系统平均调用耗时（毫秒，包含 AMA / Mica / PLC / TM / LocationLock）
        /// </summary>
        public Dictionary<string, double> SubsystemAvgLatencies { get; set; } = new();

        /// <summary>
        /// P0: 人工非计划干预次数与 MTBA 指标支撑数据
        /// </summary>
        public int ManualInterventionCount { get; set; }

        /// <summary>
        /// 综合平均稼动率百分比 (0.0 ~ 100.0)
        /// </summary>
        public double AverageUtilizationRate { get; set; }
    }

    /// <summary>
    /// 统计耗时度量对象（包含均值、上下极限与样本数）
    /// </summary>
    public class DurationMetricDto
    {
        /// <summary>样本数量</summary>
        public int SampleCount { get; set; }

        /// <summary>平均时长（毫秒）</summary>
        public double AverageMs { get; set; }

        /// <summary>下极限 / 最小用时（毫秒）</summary>
        public long MinMs { get; set; }

        /// <summary>上极限 / 最大用时（毫秒）</summary>
        public long MaxMs { get; set; }

        /// <summary>P95 分位用时（毫秒）</summary>
        public double P95Ms { get; set; }
    }
}
