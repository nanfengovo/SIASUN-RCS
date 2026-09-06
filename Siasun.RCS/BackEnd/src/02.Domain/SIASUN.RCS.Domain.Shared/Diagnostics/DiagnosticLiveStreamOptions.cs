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
        /// 是否开启 SignalR 诊断监控推流（规范核心属性，对齐 AGENTS.md §三.4）
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 是否开启诊断推流（兼容别名，与 Enabled 等价）
        /// </summary>
        public bool IsEnabled
        {
            get => Enabled;
            set => Enabled = value;
        }

        /// <summary>
        /// 采样与批量刷新节流间隔（毫秒，默认 150ms，对齐 AGENTS.md §三.4）
        /// </summary>
        public int SampleIntervalMs { get; set; } = 150;

        /// <summary>
        /// 批量推流节流间隔毫秒数（兼容别名，与 SampleIntervalMs 等价）
        /// </summary>
        public int FlushIntervalMs
        {
            get => SampleIntervalMs;
            set => SampleIntervalMs = value;
        }

        /// <summary>
        /// 环形缓冲区最大事件缓存容量（默认 100 条，对齐 AGENTS.md §三.4）
        /// </summary>
        public int MaxBufferedEvents { get; set; } = 100;

        /// <summary>
        /// 每个 Topic 维护的内存环形缓存容量（兼容别名，与 MaxBufferedEvents 等价）
        /// </summary>
        public int RingBufferCapacity
        {
            get => MaxBufferedEvents;
            set => MaxBufferedEvents = value;
        }

        /// <summary>
        /// 默认推流的最低日志/事件级别（Information | Warning | Error）
        /// </summary>
        public string MinLogLevel { get; set; } = "Information";

        /// <summary>
        /// 最大活跃动态主题数量（如 task:xxx, vehicle:xxx，默认 500 个，超出按 LRU 自动淘汰）
        /// </summary>
        public int MaxActiveTopics { get; set; } = 500;

        /// <summary>
        /// 待发送批处理缓冲队列最大容量（默认 5000 条，提供背压保护）
        /// </summary>
        public int MaxPendingQueueSize { get; set; } = 5000;

        /// <summary>
        /// 是否允许现场内网移动平板免 OAuth 认证直接连接调试（生产安全默认 false）
        /// </summary>
        public bool AllowAnonymousForLocalNetwork { get; set; } = false;

        /// <summary>
        /// SignalR Hub 路由端点
        /// </summary>
        public string HubEndpoint { get; set; } = "/signalr-hubs/diagnostics";
    }
}
