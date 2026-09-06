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
        /// 当前待消费的特权溢出条目数
        /// </summary>
        int PendingSpillCount { get; }

        /// <summary>
        /// 累计应急落盘本地磁盘写入失败次数（大于 0 意味着磁盘写保护或 I/O 故障）
        /// </summary>
        long SpillDiskWriteFailures { get; }

        /// <summary>
        /// 审计日志通道当前待写入积压总深度
        /// </summary>
        int TotalQueueCount { get; }

        /// <summary>
        /// 从本地磁盘回放恢复未入库的溢出日志
        /// </summary>
        /// <returns>恢复条目数</returns>
        int RecoverDiskSpills();
    }
}
