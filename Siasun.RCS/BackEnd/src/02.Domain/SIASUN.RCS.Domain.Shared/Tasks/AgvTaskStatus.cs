namespace SIASUN.RCS.Tasks
{
    /// <summary>
    /// AGV 调度任务粗粒度生命周期状态枚举（严格遵循 SIASUN RCS 5 状态设计）
    /// </summary>
    public enum AgvTaskStatus
    {
        /// <summary>
        /// 待处理 / 已生成待派发
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 执行中
        /// </summary>
        Running = 1,

        /// <summary>
        /// 已成功完成
        /// </summary>
        Succeeded = 2,

        /// <summary>
        /// 已失败
        /// </summary>
        Failed = 3,

        /// <summary>
        /// 已取消 / 调度员人工终止
        /// </summary>
        Canceled = 4
    }
}
