namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 库位锁与硬件联锁全局约束常量
    /// 双数据库字段长度限长与默认配置基线
    /// </summary>
    public static class LocationLockConsts
    {
        /// <summary>
        /// 库位编码最大长度
        /// </summary>
        public const int MaxLocationCodeLength = 64;

        /// <summary>
        /// 绑定的车辆编号最大长度
        /// </summary>
        public const int MaxVehicleCodeLength = 64;

        /// <summary>
        /// 锁持有原因说明最大长度
        /// </summary>
        public const int MaxReasonLength = 512;

        /// <summary>
        /// PLC 网关驱动标识最大长度
        /// </summary>
        public const int MaxGatewayIdLength = 64;

        /// <summary>
        /// PLC 信号 Tag 名称最大长度
        /// </summary>
        public const int MaxTagLength = 128;

        /// <summary>
        /// 人工维护封锁占位车辆编号
        /// </summary>
        public const string MaintenanceVehicleCode = "SYSTEM_MAINTENANCE";
    }
}
