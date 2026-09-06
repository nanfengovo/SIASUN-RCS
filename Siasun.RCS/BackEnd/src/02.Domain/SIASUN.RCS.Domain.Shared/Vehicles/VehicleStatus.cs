namespace SIASUN.RCS.Vehicles
{
    /// <summary>
    /// AGV 车辆运行状态枚举
    /// </summary>
    public enum VehicleStatus
    {
        /// <summary>
        /// 空闲待命
        /// </summary>
        Idle = 0,

        /// <summary>
        /// 执行任务中
        /// </summary>
        Running = 1,

        /// <summary>
        /// 故障报警 / 异常停机
        /// </summary>
        Error = 2,

        /// <summary>
        /// 充电中
        /// </summary>
        Charging = 3,

        /// <summary>
        /// 离线断联
        /// </summary>
        Offline = 4,

        /// <summary>
        /// 人工接管 / 维修调试
        /// </summary>
        Manual = 5
    }
}
