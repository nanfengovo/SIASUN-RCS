namespace SIASUN.RCS.Outbox
{
    /// <summary>
    /// Outbox 事务可靠消息状态枚举
    /// </summary>
    public enum OutboxMessageStatus
    {
        /// <summary>
        /// 待发布（事务已持久化，等待后台工作者投递）
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 投递中 / 重试中
        /// </summary>
        Publishing = 1,

        /// <summary>
        /// 已成功发布送达目标系统
        /// </summary>
        Published = 2,

        /// <summary>
        /// 达到最大重试上限，转入死信队列表备查
        /// </summary>
        DeadLetter = 3
    }
}
