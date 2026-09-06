namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR
{
    /// <summary>
    /// SignalR 实时诊断与推流中台配置选项
    /// 继承自标准 DiagnosticLiveStreamOptions（规范核心对齐），保持平滑向后兼容
    /// </summary>
    public class SignalRDiagnosticsOptions : SIASUN.RCS.Diagnostics.DiagnosticLiveStreamOptions
    {
        /// <summary>
        /// 历史遗留配置节节点名称（推荐迁移至 DiagnosticLiveStream）
        /// </summary>
        public new const string SectionName = "SignalRDiagnostics";
    }
}
