using System;
using System.Text.Json.Serialization;

namespace SIASUN.RCS.Diagnostics.FlightPack
{
    /// <summary>
    /// 黑匣子三轨时序事件统一投影模型
    /// </summary>
    public class FlightPackTimelineEvent
    {
        /// <summary>
        /// 事件全局唯一标识
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 事件发生的绝对时间戳 (UTC)
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 相对事故回溯起点的毫秒偏移 (用于时间轴 Scrubber 渲染)
        /// </summary>
        [JsonPropertyName("relativeMs")]
        public long RelativeMs { get; set; }

        /// <summary>
        /// 所属时序轨道 (API - 外部报文轨, Operator - 人工干预轨, Exception - 异常故障轨)
        /// </summary>
        [JsonPropertyName("track")]
        public string Track { get; set; } = "API";

        /// <summary>
        /// 严重性级别 (Information, Warning, Error, Fatal)
        /// </summary>
        [JsonPropertyName("level")]
        public string Level { get; set; } = "Information";

        /// <summary>
        /// 事件来源组件或外部系统 (如 MES, TM, UI, Internal)
        /// </summary>
        [JsonPropertyName("source")]
        public string Source { get; set; } = "Internal";

        /// <summary>
        /// 事件标题 / 操作动作 / 报文路径
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 事件内容摘要说明
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 统一追踪标识 (TraceId / CorrelationId)
        /// </summary>
        [JsonPropertyName("traceId")]
        public string? TraceId { get; set; }

        /// <summary>
        /// 原始日志底层索引引用
        /// </summary>
        [JsonPropertyName("rawRef")]
        public RawRefDto? RawRef { get; set; }
    }

    /// <summary>
    /// 原始数据文件索引关联 DTO
    /// </summary>
    public class RawRefDto
    {
        /// <summary>
        /// 原始日志包内相对文件路径 (如 raw/api_logs.json)
        /// </summary>
        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        /// <summary>
        /// 原始记录标识
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// 原始文本行号 (可选)
        /// </summary>
        [JsonPropertyName("line")]
        public int? Line { get; set; }
    }
}
