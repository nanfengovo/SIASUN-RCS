using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIASUN.RCS.Diagnostics.FlightPack;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics
{
    public class IncidentNarrativeBuilder : IIncidentNarrativeBuilder, ITransientDependency
    {
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
            sb.AppendLine("## ⏱️ 关键事件时序流 (Chronological Narrative)");
            sb.AppendLine();

            if (timelineEvents == null || timelineEvents.Count == 0)
            {
                sb.AppendLine("*在此时间窗口内未检索到关联时序事件。*");
                sb.AppendLine();
            }
            else
            {
                int step = 1;
                foreach (var evt in timelineEvents.OrderBy(e => e.Timestamp))
                {
                    var icon = evt.Level switch
                    {
                        "Error" or "Fatal" => "🛑",
                        "Warning" => "⚠️",
                        _ => "🔹"
                    };

                    var trackTag = evt.Track switch
                    {
                        "API" => "通信",
                        "Operator" => "操作",
                        "Exception" => "异常",
                        _ => evt.Track
                    };

                    sb.AppendLine($"{step++}. **[{evt.Timestamp:HH:mm:ss.fff}]** {icon} `[{trackTag}/{evt.Source}]` **{evt.Title}**");
                    if (!string.IsNullOrWhiteSpace(evt.Summary))
                    {
                        sb.AppendLine($"   - *详情*：{evt.Summary}");
                    }
                    if (!string.IsNullOrWhiteSpace(evt.TraceId))
                    {
                        sb.AppendLine($"   - *链路 TraceId*：`{evt.TraceId}`");
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
    }
}
