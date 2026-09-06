namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// API 报文审计日志通道领域端口契约
    /// 提供特权审计证据溢流监控与队列积压观测能力
    /// </summary>
    public interface IApiAuditLogChannel
    {
        /// <summary>
        /// 特权审计证据应急溢流落盘累计计数
        /// </summary>
        long SpillCount { get; }

        /// <summary>
        /// 审计日志通道当前待写入积压总深度
        /// </summary>
        int TotalQueueCount { get; }
    }
}
