using System.Collections.Generic;

namespace SIASUN.RCS.Diagnostics.AI
{
    /// <summary>
    /// AI 事故深度根因智能推理诊断输出数据传输对象
    /// </summary>
    public class AiAnalysisResultDto
    {
        /// <summary>
        /// 推理调用是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 本次推理使用的模型标识（如 deepseek-r1:7b）
        /// </summary>
        public string ModelUsed { get; set; } = string.Empty;

        /// <summary>
        /// 一句话核心根因推断总结
        /// </summary>
        public string RootCauseSummary { get; set; } = string.Empty;

        /// <summary>
        /// 根因结论置信度评级 (High - 高置信度, Medium - 中等置信度, Low - 低置信度需人工核实)
        /// </summary>
        public string ConfidenceLevel { get; set; } = "Medium";

        /// <summary>
        /// 初步定责归属 (如: 调度算法/路径死锁、现场误操作/人工干预、硬件通信/车体故障、上游对接/下发异常)
        /// </summary>
        public string ResponsibleParty { get; set; } = "未确定";

        /// <summary>
        /// 针对当前事故场景的优先级处置与排障建议清单
        /// </summary>
        public List<string> RecommendedActions { get; set; } = new();

        /// <summary>
        /// 大模型生成的完整 Markdown 诊断报告正文
        /// </summary>
        public string MarkdownReport { get; set; } = string.Empty;

        /// <summary>
        /// 大模型原始 HTTP 响应载荷（备查溯源证据）
        /// </summary>
        public string RawResponse { get; set; } = string.Empty;

        /// <summary>
        /// AI 诊断推理耗时 (毫秒)
        /// </summary>
        public long ElapsedMs { get; set; }

        /// <summary>
        /// 推理失败或超时时的错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
