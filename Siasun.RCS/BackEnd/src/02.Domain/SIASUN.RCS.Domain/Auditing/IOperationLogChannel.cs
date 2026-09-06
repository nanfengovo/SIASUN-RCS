namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// 操作审计日志通道领域契约
    /// 负责暴露调度员操作轨道的积压深度与特权溢流保全指标
    /// </summary>
    public interface IOperationLogChannel
    {
        /// <summary>
        /// 累计特权溢出保全总次数
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
        /// 综合队列当前积压深度
        /// </summary>
        int TotalQueueCount { get; }

        /// <summary>
        /// 从本地磁盘回放未消费的溢出操作日志
        /// </summary>
        /// <returns>回放恢复的条目数</returns>
        int RecoverDiskSpills();
    }
}
