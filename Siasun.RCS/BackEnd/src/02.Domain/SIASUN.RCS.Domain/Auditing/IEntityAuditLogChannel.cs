namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// 实体变更审计日志异步通道接口
    /// 负责将变更实体异步暂存并由后台工作服务批量持久化，支持特权核心聚合根零丢失与紧急溢出保全
    /// </summary>
    public interface IEntityAuditLogChannel
    {
        /// <summary>
        /// 尝试将实体变更审计消息推入通道队列（特权核心实体在通道拥堵时自动进入溢出保全环）
        /// </summary>
        /// <param name="message">实体变更审计消息</param>
        /// <returns>是否成功入队或保全</returns>
        bool TryWrite(EntityAuditLogMessage message);

        /// <summary>
        /// 异步向通道写入实体变更消息，特权实体在通道满载时异步等待槽位并支持溢出保全
        /// </summary>
        /// <param name="message">实体变更审计消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>值任务</returns>
        System.Threading.Tasks.ValueTask WriteAsync(EntityAuditLogMessage message, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// 累计特权溢出保全数量（大于 0 说明发生过极端通道饱和）
        /// </summary>
        long SpillCount { get; }

        /// <summary>
        /// 当前待消费的特权溢出条目数
        /// </summary>
        int PendingSpillCount { get; }

        /// <summary>
        /// 当前综合队列等待深度（含特权、常规与溢出环待消费数）
        /// </summary>
        int TotalQueueCount { get; }

        /// <summary>
        /// 从本地磁盘回放恢复未入库的溢出日志
        /// </summary>
        /// <returns>恢复条目数</returns>
        int RecoverDiskSpills();
    }
}
