namespace SIASUN.RCS.Batches
{
    /// <summary>
    /// AGV 批次管理与多车编排状态枚举
    /// 遵循《AGENTS.md》铁律 6：支持一单分拆多子任务、多车协同、汇聚同步与载具绑定
    /// </summary>
    public enum AgvBatchStatus
    {
        /// <summary>
        /// 批次已创建，等待拆单编排
        /// </summary>
        Created = 0,

        /// <summary>
        /// 批次已拆分子任务，派发执行中
        /// </summary>
        Dispatching = 1,

        /// <summary>
        /// 部分子任务已完成
        /// </summary>
        PartiallyCompleted = 2,

        /// <summary>
        /// 全部子任务已完成，批次汇聚同步成功
        /// </summary>
        Completed = 3,

        /// <summary>
        /// 批次执行失败
        /// </summary>
        Failed = 4,

        /// <summary>
        /// 批次已被人为取消
        /// </summary>
        Canceled = 5
    }
}
