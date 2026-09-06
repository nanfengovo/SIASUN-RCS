namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// 实体变更审计日志异步通道接口
    /// 负责将变更实体异步暂存并由后台工作服务批量持久化，支持特权核心聚合根零丢失
    /// </summary>
    public interface IEntityAuditLogChannel
    {
        /// <summary>
        /// 尝试将实体变更审计消息推入通道队列
        /// </summary>
        /// <param name="message">实体变更审计消息</param>
        /// <returns>是否成功入队</returns>
        bool TryWrite(EntityAuditLogMessage message);
    }
}
