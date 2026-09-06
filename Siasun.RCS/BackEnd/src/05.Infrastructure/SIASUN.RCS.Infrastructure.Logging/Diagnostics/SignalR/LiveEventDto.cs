using System;
using System.Text.Json.Serialization;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR
{
    /// <summary>
    /// SignalR 实时推流监控事件数据传输对象
    /// </summary>
    public class LiveEventDto
    {
        /// <summary>
        /// 事件全局唯一标识
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 事件产生的 UTC 时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 所属时序轨道 (API, Operator, Exception)
        /// </summary>
        [JsonPropertyName("track")]
        public string Track { get; set; } = "API";

        /// <summary>
        /// 日志或事件等级 (Information, Warning, Error, Fatal)
        /// </summary>
        [JsonPropertyName("level")]
        public string Level { get; set; } = "Information";

        /// <summary>
        /// 事件来源 (如 MES, TM, UI, Hardware)
        /// </summary>
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 事件标题
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 事件详情摘要
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 关联追踪标识 (TraceId / CorrelationId)
        /// </summary>
        [JsonPropertyName("traceId")]
        public string? TraceId { get; set; }

        /// <summary>
        /// 目标业务标识（如任务号）
        /// </summary>
        [JsonPropertyName("targetId")]
        public string? TargetId { get; set; }

        /// <summary>
        /// 关联车辆标识
        /// </summary>
        [JsonPropertyName("vehicleId")]
        public string? VehicleId { get; set; }

        /// <summary>
        /// 转换为统一时序事件领域模型
        /// </summary>
        /// <returns>DiagnosticTimelineEvent 领域事件</returns>
        public SIASUN.RCS.Diagnostics.DiagnosticTimelineEvent ToDiagnosticEvent()
        {
            return new SIASUN.RCS.Diagnostics.DiagnosticTimelineEvent
            {
                Id = Id,
                Timestamp = Timestamp,
                Track = Track,
                Level = Level,
                Source = Source,
                Title = Title,
                Summary = Summary,
                TraceId = TraceId,
                TargetId = TargetId,
                VehicleId = VehicleId
            };
        }

        /// <summary>
        /// 从统一时序事件领域模型构建 SignalR 推流 DTO
        /// </summary>
        /// <param name="evt">领域时序事件</param>
        /// <returns>LiveEventDto 对象</returns>
        public static LiveEventDto FromDiagnosticEvent(SIASUN.RCS.Diagnostics.DiagnosticTimelineEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            return new LiveEventDto
            {
                Id = evt.Id,
                Timestamp = evt.Timestamp,
                Track = evt.Track,
                Level = evt.Level,
                Source = evt.Source,
                Title = evt.Title,
                Summary = evt.Summary,
                TraceId = evt.TraceId,
                TargetId = evt.TargetId,
                VehicleId = evt.VehicleId
            };
        }
    }
}
