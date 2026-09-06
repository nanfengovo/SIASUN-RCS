using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIASUN.RCS.Diagnostics;
using SIASUN.RCS.Diagnostics.FlightPack;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics
{
    /// <summary>
    /// 黑匣子离线报告 Markdown 叙事生成器
    /// 将客观时序事件与人为操作日志综合提炼为因果时序与排障建议报告
    /// </summary>
    public class IncidentNarrativeBuilder : IIncidentNarrativeBuilder, ITransientDependency
    {
        /// <summary>
        /// 基于黑匣子元数据与多轨时序事件构建 Markdown 格式的排障叙事报告
        /// </summary>
        /// <param name="metadata">黑匣子元数据</param>
        /// <param name="timelineEvents">时序事件集合</param>
        /// <returns>格式化的 Markdown 叙事报告正文</returns>
        public string BuildMarkdownNarrative(FlightPackMetadata metadata, IReadOnlyList<FlightPackTimelineEvent> timelineEvents)
        {
            var sb = new StringBuilder();

            var anchorKey = metadata.Anchor?.Key ?? "Unknown";
            var vehicleId = metadata.Anchor?.RelatedVehicleId ?? "N/A";
            var finalStatus = metadata.Anchor?.TaskLifecycle?.FinalStatus ?? "Unknown";
            var timeWindow = metadata.TimeWindow;

            sb.AppendLine("# 📋 RCS 任务排障黑匣子摘要报告");
            sb.AppendLine();
            sb.AppendLine($"- **锚点类型**：{metadata.Anchor?.Type ?? "Task"}");
            sb.AppendLine($"- **目标标识**：{anchorKey}");
            sb.AppendLine($"- **关联车辆**：{vehicleId}");
            sb.AppendLine($"- **最终状态**：{finalStatus}");
            sb.AppendLine($"- **取证时间窗口**：{timeWindow.QueryStartTime:yyyy-MM-dd HH:mm:ss} ~ {timeWindow.QueryEndTime:yyyy-MM-dd HH:mm:ss} (UTC)");
            sb.AppendLine($"- **取证导出人员**：{metadata.ExportContext?.ExportedByUserName ?? "System"} (IP: {metadata.ExportContext?.ClientIp})");
            sb.AppendLine($"- **导出时间**：{metadata.ExportContext?.ExportTime:yyyy-MM-dd HH:mm:ss} (UTC)");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // 1. 关键事件时序流
            // 1. 关键事件时序流（支持周期心跳自动降噪折叠与超长事件截断）
            sb.AppendLine("## ⏱️ 关键事件时序流 (Chronological Narrative)");
            sb.AppendLine();

            if (timelineEvents == null || timelineEvents.Count == 0)
            {
                sb.AppendLine("*在此时间窗口内未检索到关联时序事件。*");
                sb.AppendLine();
            }
            else
            {
                var foldedEvents = FoldFlappingAndFlicker(FoldHeartbeatsAndPings(timelineEvents.OrderBy(e => e.Timestamp).ToList()));
                int step = 1;
                const int maxNarrativeEvents = 200;

                if (foldedEvents.Count <= maxNarrativeEvents)
                {
                    foreach (var evt in foldedEvents)
                    {
                        RenderTimelineEvent(sb, ref step, evt);
                    }
                }
                else
                {
                    // 超过上限时，保留前后各 100 条关键事件，中间截断提示
                    var headEvents = foldedEvents.Take(100);
                    var tailEvents = foldedEvents.Skip(foldedEvents.Count - 100).Take(100);
                    var skippedCount = foldedEvents.Count - 200;

                    foreach (var evt in headEvents)
                    {
                        RenderTimelineEvent(sb, ref step, evt);
                    }

                    sb.AppendLine($"... *[已自动省略中间 {skippedCount} 条常规事件，完整全时序请查阅离线包 timeline.json]* ...");
                    sb.AppendLine();

                    foreach (var evt in tailEvents)
                    {
                        RenderTimelineEvent(sb, ref step, evt);
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();

            // 2. 异常与关键线索排查
            sb.AppendLine("## 🔍 异常与关键线索排查");
            sb.AppendLine();

            var anomalies = timelineEvents?
                .Where(e => e.Level == "Error" || e.Level == "Fatal" || e.Level == "Warning")
                .Where(e => string.Equals(e.Level, DiagnosticLevels.Error, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(e.Level, DiagnosticLevels.Fatal, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(e.Level, DiagnosticLevels.Warning, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Timestamp)
                .ToList() ?? new List<FlightPackTimelineEvent>();

            if (anomalies.Count == 0)
            {
                sb.AppendLine("- **检测结果**：时间窗口内未捕获到 Warning 或 Error 级异常事件，流程可能正常结束或处于静默等待中。");
            }
            else
            {
                var firstAnomaly = anomalies.First();
                sb.AppendLine($"- **第一多米诺骨牌 (最早异常触发点)**：发生在 `[{firstAnomaly.Timestamp:HH:mm:ss.fff}]`，来源为 `[{firstAnomaly.Source}]`：**{firstAnomaly.Title}**。");

                var operatorEvents = timelineEvents?
                    .Where(e => e.Track == "Operator")
                    .Where(e => string.Equals(e.Track, DiagnosticTracks.Operator, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                if (operatorEvents != null && operatorEvents.Count > 0)
                {
                    var lastOp = operatorEvents.Last();
                    if (lastOp.Timestamp > firstAnomaly.Timestamp)
                    {
                        sb.AppendLine($"- **人为介入分析**：操作员在最早异常触发之后介入（`[{lastOp.Timestamp:HH:mm:ss.fff}]` 执行了「{lastOp.Title}」），属于**已知故障后的运维处置行为**，而非引发故障的第一原因。");
                    }
                    else
                    {
                        sb.AppendLine($"- **人为介入分析**：检测到人工操作（`[{lastOp.Timestamp:HH:mm:ss.fff}]` 执行了「{lastOp.Title}」）先于系统报警出现，需重点核实是否由于现场误操作引发后续异常。");
                    }
                }

                sb.AppendLine("- **异常事件清单**：");
                foreach (var a in anomalies)
                {
                    sb.AppendLine($"  - `[{a.Timestamp:HH:mm:ss.fff}]` [{a.Track}] {a.Title}: {a.Summary}");
                }
            }
            sb.AppendLine();

            sb.AppendLine("---");
            sb.AppendLine();

            // 3. 建议排查清单
            sb.AppendLine("## 🛠️ 建议排查清单");
            sb.AppendLine();
            sb.AppendLine("1. **通信与接口排查**：检查 MES/TM 与 RCS 之间的网络延迟及丢包情况，关注上述异常发生的端点。");
            sb.AppendLine("2. **工位硬件与车辆状态**：核实对应车辆在异常发生时所处物理点位，检查避障传感器、雷达或库位光电是否异常。");
            sb.AppendLine("3. **人工操作定性**：核实操作记录中的点击原因备注，确保现场操作合规。");
            sb.AppendLine();

            sb.AppendLine("---");
            sb.AppendLine();

            // 4. 离线 LLM 对话提示词模板
            sb.AppendLine("> 💡 **【大模型离线提问提示词】**");
            sb.AppendLine("> *如果你需要使用外部大模型（如 ChatGPT / Claude / DeepSeek）做进一步根因研判，可直接将以下引用块中的内容复制给大模型：*");
            sb.AppendLine(">");
            sb.AppendLine("> ```text");
            sb.AppendLine("> 你是新松 RCS 资深可靠性与排障专家。");
            sb.AppendLine("> 请基于以下排障黑匣子提取的案发事实，客观分析故障根本原因，并给出责任推断与解决建议。");
            sb.AppendLine("> 注意：给出候选根因排序及证据引用，不要盲目定性。");
            sb.AppendLine(">");
            sb.AppendLine($"> [事故基本信息] 任务: {anchorKey}, 车辆: {vehicleId}, 状态: {finalStatus}");
            sb.AppendLine($"> [关键异常事件数量] 共捕获 {anomalies.Count} 起 Warning/Error 级别事件。");
            if (anomalies.Count > 0)
            {
                sb.AppendLine($"> [最早异常触发] 时间: {anomalies.First().Timestamp:HH:mm:ss.fff}, 内容: {anomalies.First().Title}");
            }
            sb.AppendLine("> ```");

            return sb.ToString();
        }

        private static List<FoldedTimelineItem> FoldHeartbeatsAndPings(List<FlightPackTimelineEvent> sortedEvents)
        {
            var result = new List<FoldedTimelineItem>();
            if (sortedEvents == null || sortedEvents.Count == 0) return result;

            FoldedTimelineItem? currentFold = null;

            foreach (var evt in sortedEvents)
            {
                bool isPeriodic = IsPeriodicHeartbeat(evt);

                if (isPeriodic && currentFold != null &&
                    string.Equals(currentFold.Track, evt.Track, System.StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(currentFold.Source, evt.Source, System.StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(currentFold.Title, evt.Title, System.StringComparison.OrdinalIgnoreCase))
                {
                    // 累积折叠
                    currentFold.EndTime = evt.Timestamp;
                    currentFold.FoldCount++;
                }
                else
                {
                    if (currentFold != null)
                    {
                        result.Add(currentFold);
                        currentFold = null;
                    }

                    if (isPeriodic)
                    {
                        currentFold = new FoldedTimelineItem
                        {
                            StartTime = evt.Timestamp,
                            EndTime = evt.Timestamp,
                            Track = evt.Track,
                            Level = evt.Level,
                            Source = evt.Source,
                            Title = evt.Title,
                            Summary = evt.Summary,
                            TraceId = evt.TraceId,
                            FoldCount = 1
                        };
                    }
                    else
                    {
                        result.Add(new FoldedTimelineItem
                        {
                            StartTime = evt.Timestamp,
                            EndTime = evt.Timestamp,
                            Track = evt.Track,
                            Level = evt.Level,
                            Source = evt.Source,
                            Title = evt.Title,
                            Summary = evt.Summary,
                            TraceId = evt.TraceId,
                            FoldCount = 1
                        });
                    }
                }
            }

            if (currentFold != null)
            {
                result.Add(currentFold);
            }

            return result;
        }

        private static bool IsPeriodicHeartbeat(FlightPackTimelineEvent evt)
        {
            if (evt.Level == "Error" || evt.Level == "Fatal" || evt.Level == "Warning")
            {
                return false;
            }

            return evt.Title.Contains("Heartbeat", System.StringComparison.OrdinalIgnoreCase)
                || evt.Title.Contains("心跳", System.StringComparison.OrdinalIgnoreCase)
                || evt.Title.Contains("Ping", System.StringComparison.OrdinalIgnoreCase)
                || evt.Title.Contains("KeepAlive", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(evt.Track, "Telemetry", System.StringComparison.OrdinalIgnoreCase);
        }

        private static List<FoldedTimelineItem> FoldFlappingAndFlicker(List<FoldedTimelineItem> items)
        {
            if (items.Count < 3) return items;

            var result = new List<FoldedTimelineItem>();
            int i = 0;
            while (i < items.Count)
            {
                var current = items[i];
                int j = i + 1;
                while (j < items.Count &&
                       string.Equals(items[j].Source, current.Source, StringComparison.OrdinalIgnoreCase) &&
                       (items[j].StartTime - items[j - 1].EndTime).TotalSeconds <= 15.0)
                {
                    j++;
                }

                int count = j - i;
                if (count >= 3)
                {
                    var first = items[i];
                    var last = items[j - 1];
                    var distinctTitles = items.Skip(i).Take(count).Select(x => x.Title).Distinct().ToList();

                    var highestLevel = items.Skip(i).Take(count).Any(x => x.Level == "Error" || x.Level == "Fatal") ? "Error"
                        : items.Skip(i).Take(count).Any(x => x.Level == "Warning") ? "Warning" : first.Level;

                    result.Add(new FoldedTimelineItem
                    {
                        StartTime = first.StartTime,
                        EndTime = last.EndTime,
                        Track = first.Track,
                        Level = highestLevel,
                        Source = first.Source,
                        Title = $"{first.Source} 状态频繁抖动/信号震荡",
                        Summary = $"在短时间 ({(last.EndTime - first.StartTime).TotalSeconds:F1}s) 内检测到高频状态震荡跳变 (累计 {count} 次)，交替事件包括: [{string.Join(" / ", distinctTitles)}]。已自动降维折叠，建议重点排查传感器对焦、工位机械抖动或电气接触不稳定。",
                        TraceId = first.TraceId,
                        FoldCount = count,
                        IsFlapping = true
                    });

                    i = j;
                }
                else
                {
                    result.Add(current);
                    i++;
                }
            }

            return result;
        }

        private static void RenderTimelineEvent(StringBuilder sb, ref int step, FoldedTimelineItem evt)
        {
            var icon = evt.Level switch
            {
                "Error" or "Fatal" => "🛑",
                "Warning" => "⚠️",
                var l when string.Equals(l, DiagnosticLevels.Error, StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(l, DiagnosticLevels.Fatal, StringComparison.OrdinalIgnoreCase) => "🛑",
                var l when string.Equals(l, DiagnosticLevels.Warning, StringComparison.OrdinalIgnoreCase) => "⚠️",
                _ => "🔹"
            };

            var trackTag = evt.Track switch
            {
                "API" => "通信",
                "Operator" => "操作",
                "Exception" => "异常",
                var t when string.Equals(t, DiagnosticTracks.Api, StringComparison.OrdinalIgnoreCase) => "通信",
                var t when string.Equals(t, DiagnosticTracks.Operator, StringComparison.OrdinalIgnoreCase) => "操作",
                var t when string.Equals(t, DiagnosticTracks.Exception, StringComparison.OrdinalIgnoreCase) => "异常",
                var t when string.Equals(t, DiagnosticTracks.Entity, StringComparison.OrdinalIgnoreCase) => "实体",
                var t when string.Equals(t, DiagnosticTracks.Vehicle, StringComparison.OrdinalIgnoreCase) => "车辆",
                _ => evt.Track
            };

            if (evt.IsFlapping)
            {
                sb.AppendLine($"{step++}. **[{evt.StartTime:HH:mm:ss.fff} ~ {evt.EndTime:HH:mm:ss.fff}]** ⚡ `[{trackTag}/{evt.Source}]` **{evt.Title}（{evt.FoldCount} 次跳变，已智能降维折叠）**");
                if (!string.IsNullOrWhiteSpace(evt.Summary))
                {
                    sb.AppendLine($"   - *分析*：{evt.Summary}");
                }
            }
            else if (evt.FoldCount > 1)
            {
                sb.AppendLine($"{step++}. **[{evt.StartTime:HH:mm:ss.fff} ~ {evt.EndTime:HH:mm:ss.fff}]** 🔹 `[{trackTag}/{evt.Source}]` **{evt.Title}（连续 {evt.FoldCount} 次采样，已自动折叠降噪）**");
            }
            else
            {
                sb.AppendLine($"{step++}. **[{evt.StartTime:HH:mm:ss.fff}]** {icon} `[{trackTag}/{evt.Source}]` **{evt.Title}**");
            }

            if (!string.IsNullOrWhiteSpace(evt.Summary) && evt.FoldCount == 1 && !evt.IsFlapping)
            {
                sb.AppendLine($"   - *详情*：{evt.Summary}");
            }
            if (!string.IsNullOrWhiteSpace(evt.TraceId) && !evt.IsFlapping)
            {
                sb.AppendLine($"   - *链路 TraceId*：`{evt.TraceId}`");
            }
        }

        private class FoldedTimelineItem
        {
            public System.DateTime StartTime { get; set; }
            public System.DateTime EndTime { get; set; }
            public string Track { get; set; } = string.Empty;
            public string Level { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string? Summary { get; set; }
            public string? TraceId { get; set; }
            public int FoldCount { get; set; } = 1;
            public bool IsFlapping { get; set; }
        }
    }
}
