namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 实时诊断推流队列遥测指标提供者端口
    /// </summary>
    public interface ILiveStreamTelemetryProvider
    {
        /// <summary>
        /// 当前待推流事件积压数
        /// </summary>
        int PendingCount { get; }
    }
}
