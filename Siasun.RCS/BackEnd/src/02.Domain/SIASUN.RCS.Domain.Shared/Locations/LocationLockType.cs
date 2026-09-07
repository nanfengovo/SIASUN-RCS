namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 物理空间与库位锁类型枚举
    /// 区分搬运作业的不同物理占用语义与人工维护状态
    /// </summary>
    public enum LocationLockType
    {
        /// <summary>
        /// 取料锁（Fetch）：小车正前往或正在该库位取走载具/物料（排他锁）
        /// </summary>
        Fetch = 1,

        /// <summary>
        /// 放料锁（Put）：小车正前往或正在将物料放置到该目标库位（排他锁）
        /// </summary>
        Put = 2,

        /// <summary>
        /// 通行/关键区锁（Transit）：窄道、风淋门、单向行驶交汇点等物理瓶颈区域（交通管制排他锁）
        /// </summary>
        Transit = 3,

        /// <summary>
        /// 人工维护封锁（Maintenance）：调度员人工报修或清洁封锁（无限期租约，排斥所有作业，仅支持显式解封）
        /// </summary>
        Maintenance = 4
    }
}
