namespace SIASUN.RCS.Monitor
{
    /// <summary>
    /// 系统容量健康评级
    /// </summary>
    public enum CapacityHealthLevel
    {
        /// <summary>
        /// 健康稳定（容量与日志行数处于绿色安全水位）
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// 预警（触及预警阈值，提示运维人员关注或规划扩容）
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 严重告警（触及高水位红线，必须立即触发自愈自清理或人工介入）
        /// </summary>
        Critical = 2
    }
}
