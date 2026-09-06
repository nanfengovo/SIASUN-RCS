using System;
using SIASUN.RCS.Diagnostics.FlightPack;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 调度与诊断全时序统一事件领域模型
    /// 抽象贯穿实时推流 (SignalR)、黑匣子回溯 (.rcspack) 与离线根因分析的通用时序事件
    /// </summary>
    public class DiagnosticTimelineEvent
    {
        /// <summary>
        /// 事件唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 事件发生的绝对时间戳 (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 所属时序轨道（如 API 报文轨, Operator 操作轨, Exception 异常轨, Entity 状态轨）
        /// </summary>
        public string Track { get; set; } = "API";

        /// <summary>
        /// 事件等级 (Information, Warning, Error, Fatal)
        /// </summary>
        public string Level { get; set; } = "Information";

        /// <summary>
        /// 事件来源（如 MES, TM, UI, System, Dispatcher）
        /// </summary>
        public string Source { get; set; } = "Internal";

        /// <summary>
        /// 事件简要标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 事件内容详情或参数摘要
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 统一追踪锚点标识 (TraceId / CorrelationId)
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// 目标业务对象标识（如任务编号 TaskId）
        /// </summary>
        public string? TargetId { get; set; }

        /// <summary>
        /// 关联的 AGV 车体标识（如车体编号 AgvId）
        /// </summary>
        public string? VehicleId { get; set; }

        /// <summary>
        /// 底层原始日志引用信息（可选）
        /// </summary>
        public RawRefDto? RawRef { get; set; }

        /// <summary>
        /// 将当前统一时序事件转换为黑匣子时序事件投影
        /// </summary>
        /// <param name="relativeMs">相对起始点毫秒偏移</param>
        /// <returns>黑匣子时序事件对象</returns>
        public FlightPackTimelineEvent ToFlightPackEvent(long relativeMs = 0)
        {
            return new FlightPackTimelineEvent
            {
                Id = Id,
                Timestamp = Timestamp,
                RelativeMs = relativeMs,
                Track = Track,
                Level = Level,
                Source = Source,
                Title = Title,
                Summary = Summary,
                TraceId = TraceId,
                RawRef = RawRef
            };
        }

        /// <summary>
        /// 从黑匣子时序事件构建领域统一时序事件
        /// </summary>
        /// <param name="flightPackEvent">黑匣子时序事件源</param>
        /// <returns>统一时序事件领域模型</returns>
        public static DiagnosticTimelineEvent FromFlightPackEvent(FlightPackTimelineEvent flightPackEvent)
        {
            if (flightPackEvent == null) throw new ArgumentNullException(nameof(flightPackEvent));

            return new DiagnosticTimelineEvent
            {
                Id = flightPackEvent.Id,
                Timestamp = flightPackEvent.Timestamp,
                Track = flightPackEvent.Track,
                Level = flightPackEvent.Level,
                Source = flightPackEvent.Source,
                Title = flightPackEvent.Title,
                Summary = flightPackEvent.Summary,
                TraceId = flightPackEvent.TraceId,
                RawRef = flightPackEvent.RawRef
            };
        }
    }
}

