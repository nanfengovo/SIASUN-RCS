namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR
{
    /// <summary>
    /// SignalR 实时诊断与推流中台配置选项
    /// </summary>
    public class SignalRDiagnosticsOptions
    {
        /// <summary>
        /// 配置节节点名称
        /// </summary>
        public const string SectionName = "SignalRDiagnostics";

        /// <summary>
        /// 是否开启 SignalR 诊断与实时看板推流（关闭时零资源开销）
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 批量推流节流间隔（毫秒，默认 150ms，防止网络与移动前端渲染风暴）
        /// </summary>
        public int FlushIntervalMs { get; set; } = 150;

        /// <summary>
        /// 每个 Topic 维护的内存环形缓存容量（连入即推历史记录数，默认 100）
        /// </summary>
        public int RingBufferCapacity { get; set; } = 100;

        /// <summary>
        /// 默认推流的最低日志/事件级别（Information | Warning | Error）
        /// </summary>
        public string MinLogLevel { get; set; } = "Information";

        /// <summary>
        /// 最大活跃动态主题数量（如 task:xxx, vehicle:xxx，默认 500 个，超出按 LRU 自动淘汰）
        /// </summary>
        public int MaxActiveTopics { get; set; } = 500;

        /// <summary>
        /// 待发送批处理缓冲队列最大容量（默认 5000 条，提供背压保护，防止网络慢客户端打爆内存）
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
