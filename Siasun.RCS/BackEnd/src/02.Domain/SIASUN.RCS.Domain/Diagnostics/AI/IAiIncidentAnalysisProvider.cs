using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Diagnostics.FlightPack;

namespace SIASUN.RCS.Diagnostics.AI
{
    /// <summary>
    /// AI 事故深度根因推理诊断引擎领域服务端口
    /// </summary>
    public interface IAiIncidentAnalysisProvider
    {
        /// <summary>
        /// 是否启用了 AI 诊断模块
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 基于黑匣子全景参数、三轨客观时序事件以及基础规则叙事执行大模型深度根因推理分析
        /// </summary>
        /// <param name="metadata">黑匣子元数据</param>
        /// <param name="events">时序事件集合</param>
        /// <param name="baseNarrative">规则引擎基础叙事报告</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>AI 智能推理分析结果（含根因、责任归属、建议措施与置信度）</returns>
        Task<AiAnalysisResultDto> AnalyzeIncidentAsync(
            FlightPackMetadata metadata,
            IReadOnlyList<FlightPackTimelineEvent> events,
            string baseNarrative,
            CancellationToken ct = default);
    }
}

