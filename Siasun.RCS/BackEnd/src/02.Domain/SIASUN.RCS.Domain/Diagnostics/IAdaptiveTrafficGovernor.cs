namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 自适应限流与背压降采样控制器接口（L4 工业自治抗突发保护核心契约）
    /// </summary>
    public interface IAdaptiveTrafficGovernor
    {
        /// <summary>
        /// 判定当前事件是否准入持久化或推流广播
        /// 确保 Warning/Error/Fatal 异常与 Operation 人工干预 100% 绝对放行
        /// </summary>
        /// <param name="category">事件分类（如 "Telemetry", "ApiAudit", "Operation", "EntityAudit"）</param>
        /// <param name="level">事件等级（如 "Trace", "Debug", "Info", "Warning", "Error", "Fatal"）</param>
        /// <returns>采样判定结果</returns>
        TrafficSamplingDecision ShouldAdmit(string category, string level);

        /// <summary>
        /// 获取当前自适应限流状态指标快照
        /// </summary>
        /// <returns>运行时指标快照</returns>
        TrafficGovernorMetrics GetMetrics();

        /// <summary>
        /// 重置限流统计计数器
        /// </summary>
        void Reset();
    }
}
