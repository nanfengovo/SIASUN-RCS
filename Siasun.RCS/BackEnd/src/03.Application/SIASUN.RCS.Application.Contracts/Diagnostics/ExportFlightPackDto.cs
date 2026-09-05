using System;
using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 导出黑匣子飞行排障数据包请求参数
    /// </summary>
    public class ExportFlightPackDto
    {
        /// <summary>
        /// 排障定位锚点类型 (可选: Task - 任务号, Vehicle - 车号, TimeRange - 纯时间段)
        /// </summary>
        [Required]
        public string AnchorType { get; set; } = "Task";

        /// <summary>
        /// 锚点键值 (如任务编号 "T-20260904-001" 或车辆编号 "AGV-01")
        /// </summary>
        [Required]
        public string AnchorKey { get; set; } = string.Empty;

        /// <summary>
        /// 时序范围查询起始时间 (若为空则根据任务生命周期自动判定)
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 时序范围查询结束时间 (若为空则根据任务生命周期自动判定)
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 事故发生前向前回溯缓冲时间（分钟），默认 5 分钟
        /// </summary>
        public int BufferBeforeMinutes { get; set; } = 5;

        /// <summary>
        /// 事故发生后向后延迟缓冲时间（分钟），默认 5 分钟
        /// </summary>
        public int BufferAfterMinutes { get; set; } = 5;

        /// <summary>
        /// 是否启用 AI 深度根因智能分析推理（对接车间私有化大模型）
        /// </summary>
        public bool EnableAiAnalysis { get; set; } = false;
    }
}
