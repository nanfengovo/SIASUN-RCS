using System.Collections.Generic;

namespace SIASUN.RCS.Diagnostics.FlightPack
{
    /// <summary>
    /// 黑匣子客观因果时序叙事报告构建器接口
    /// </summary>
    public interface IIncidentNarrativeBuilder
    {
        /// <summary>
        /// 基于黑匣子全景参数与三轨时序事件，根据工业时序规则生成 Markdown 事故基础叙事报告
        /// </summary>
        /// <param name="metadata">黑匣子元数据</param>
        /// <param name="timelineEvents">时序事件集合</param>
        /// <returns>Markdown 格式的诊断分析报告文本</returns>
        string BuildMarkdownNarrative(FlightPackMetadata metadata, IReadOnlyList<FlightPackTimelineEvent> timelineEvents);
    }
}
