namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 实时诊断监控与流推配置选项
    /// 严格对齐 SIASUN RCS 规范三.4（DiagnosticLiveStreamOptions: Enabled / SampleIntervalMs / MaxBufferedEvents）
    /// </summary>
    public class DiagnosticLiveStreamOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "DiagnosticLiveStream";

        /// <summary>
        /// 是否开启 SignalR 诊断监控推流
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 采样与批量刷新节流间隔（毫秒，默认 150ms）
        /// </summary>
        public int SampleIntervalMs { get; set; } = 150;

        /// <summary>
        /// 环形缓冲区最大事件缓存容量（默认 100 条）
        /// </summary>
        public int MaxBufferedEvents { get; set; } = 100;
    }
}
