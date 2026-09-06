using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Diagnostics.FlightPack;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Diagnostics.AI
{
    /// <summary>
    /// 基于工业规则的本地事故根因推理诊断引擎（确定性降级 Provider）
    /// 遵循 SIASUN RCS 工业级规范：在物理断网、无在线大模型或 AI 调用失败时，提供确定性的本地因果排障推演
    /// </summary>
    public class RuleBasedIncidentAnalysisProvider : IAiIncidentAnalysisProvider, ITransientDependency
    {
        /// <summary>
        /// 本地规则诊断 Provider 始终就绪
        /// </summary>
        public bool IsEnabled => true;

        /// <summary>
        /// 基于黑匣子元数据与多轨时序事件执行本地确定性因果推演分析
        /// </summary>
        /// <param name="metadata">黑匣子元数据</param>
        /// <param name="events">时序事件集合</param>
        /// <param name="baseNarrative">基础叙事报告</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>确定性根因推理结果</returns>
        public Task<AiAnalysisResultDto> AnalyzeIncidentAsync(
            FlightPackMetadata metadata,
            IReadOnlyList<FlightPackTimelineEvent> events,
            string baseNarrative,
            CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            var operatorEvents = events.Where(e =>
                string.Equals(e.Track, DiagnosticTracks.Operator, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.Track, "Operator", StringComparison.OrdinalIgnoreCase)).ToList();

            var apiErrorEvents = events.Where(e =>
                (string.Equals(e.Track, DiagnosticTracks.Api, StringComparison.OrdinalIgnoreCase) || string.Equals(e.Track, "API", StringComparison.OrdinalIgnoreCase)) &&
                (string.Equals(e.Level, DiagnosticLevels.Error, StringComparison.OrdinalIgnoreCase) ||
                 e.Title.Contains("500") ||
                 (e.Summary != null && e.Summary.Contains("500")))).ToList();

            var exceptionEvents = events.Where(e =>
                string.Equals(e.Track, DiagnosticTracks.Exception, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.Track, "Exception", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.Level, DiagnosticLevels.Fatal, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.Level, DiagnosticLevels.Error, StringComparison.OrdinalIgnoreCase)).ToList();

            string rootCause;
            string responsibleParty;
            string confidenceLevel;
            var actions = new List<string>();
            var logicAnalysis = new StringBuilder();

            if (operatorEvents.Count > 0)
            {
                var primaryOp = operatorEvents.Last();
                responsibleParty = "现场误操作/人工干预";
                confidenceLevel = "High";
                rootCause = $"调度员在事故窗口期执行了人工干预动作【{primaryOp.Title}】，强行改变了核心调度流转状态。";
                actions.Add("核查调度台操作人员干预记录（检查 OperationLog 审计轨中的 BeforeState/AfterState 与操作原因）");
                actions.Add("评估当前物理 AGV 实际停靠工位与装载状态，防止现场物料账实不符");
                actions.Add("释放该任务占用的库位锁与路口互锁，按现场实际工序重新分配并派发新任务");

                logicAnalysis.AppendLine($"1. 调度员于时序 [{primaryOp.Timestamp:HH:mm:ss.fff}] 触发干预操作: {primaryOp.Title} ({primaryOp.Summary})。");
                logicAnalysis.AppendLine("2. 该操作直接中断了原定工作流自动推进步进，导致任务生命周期非正常收敛。");
            }
            else if (apiErrorEvents.Count > 0)
            {
                var primaryApi = apiErrorEvents.First();
                responsibleParty = "上游对接/下发异常";
                confidenceLevel = "High";
                rootCause = $"外部接口通信故障或上游系统响应异常（{primaryApi.Title}），导致指令调度链条断裂。";
                actions.Add("排查外部系统（MES/WMS/AMA）网络连通性、网关超时配置及接口鉴权凭证");
                actions.Add("对照报文审计 RawRef 检查请求报文 Payload 与异常响应堆栈");
                actions.Add("待外部服务恢复后，通过 SAGA 补偿流程或调度控制台重新触发下发");

                logicAnalysis.AppendLine($"1. 接口于 [{primaryApi.Timestamp:HH:mm:ss.fff}] 捕获通信异常: {primaryApi.Title}。");
                logicAnalysis.AppendLine("2. 上游系统未能按预期提供握手响应，触发调度主循环熔断与异常降级。");
            }
            else if (exceptionEvents.Count > 0)
            {
                var primaryEx = exceptionEvents.First();
                responsibleParty = "硬件通信/车体故障";
                confidenceLevel = "Medium";
                rootCause = $"捕获底层核心异常或车辆遥测中断（{primaryEx.Title}），导致车辆脱离正常调度状态。";
                rootCause = $"捕获底层核心异常或车辆遥测中断（{primaryEx.Title}），底盘通信超时或车载硬件上报故障，导致车辆脱离正常调度状态。";
                actions.Add("检查车载控制器 WiFi/5G 漫游信噪比，确认车体心跳报文时延");
                actions.Add("登录车体排查驱动器、激光雷达测距与急停安全回路状态");
                actions.Add("在调度台复位车辆警报并重新校准车辆世界坐标");

                logicAnalysis.AppendLine($"1. 监控系统于 [{primaryEx.Timestamp:HH:mm:ss.fff}] 记录异常事件: {primaryEx.Title}。");
                logicAnalysis.AppendLine("2. 异常导致底盘状态未按期反馈，驱动任务进入超时保护机制。");
            }
            else
            {
                responsibleParty = "调度算法/路径死锁";
                confidenceLevel = "Medium";
                rootCause = "时序中未捕获致命硬件故障与外部通信异常，推断为工位资源互锁、路径死锁或步进条件未满足。";
                actions.Add("检查当前任务的 StepIndex 与 ActiveLeg，确认任务卡滞的精确动作段");
                actions.Add("排查现场交汇路口与关键库位的占用锁状态，消除双车对头死锁");
                actions.Add("必要时通过调度控制台人工介入指定备选避让路线");

                logicAnalysis.AppendLine("1. 检查时序内各生命周期事件，外部通信与车体心跳均处于基础连通态。");
                logicAnalysis.AppendLine("2. 任务流转在特定步骤停止刷新，符合资源争用或前置硬件联锁未信号对齐特征。");
            }

            var reportSb = new StringBuilder();
            reportSb.AppendLine("【根因结论】：");
            reportSb.AppendLine(rootCause);
            reportSb.AppendLine();
            reportSb.AppendLine("【责任归属】：");
            reportSb.AppendLine(responsibleParty);
            reportSb.AppendLine();
            reportSb.AppendLine("【置信度】：");
            reportSb.AppendLine(confidenceLevel);
            reportSb.AppendLine();
            reportSb.AppendLine("【推荐排障措施】：");
            foreach (var action in actions)
            {
                reportSb.AppendLine($"- {action}");
            }
            reportSb.AppendLine();
            reportSb.AppendLine("【详细时序逻辑分析】：");
            reportSb.AppendLine(logicAnalysis.ToString().TrimEnd());

            sw.Stop();

            var result = new AiAnalysisResultDto
            {
                IsSuccess = true,
                ModelUsed = "RuleBasedDeductionEngine.v1",
                RootCauseSummary = rootCause,
                ResponsibleParty = responsibleParty,
                ConfidenceLevel = confidenceLevel,
                RecommendedActions = actions,
                MarkdownReport = reportSb.ToString(),
                RawResponse = string.Empty,
                ElapsedMs = sw.ElapsedMilliseconds
            };

            return Task.FromResult(result);
        }
    }
}
