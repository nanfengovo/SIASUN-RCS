namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 字段数据源提取类型
    /// </summary>
    public enum OptionCodeFieldSource
    {
        /// <summary>
        /// 固定常量（直接读取 Schema 中定义的 ConstValue）
        /// </summary>
        Const = 1,

        /// <summary>
        /// 任务入参（从任务创建/派发的动态参数字典中提取）
        /// </summary>
        Args = 2,

        /// <summary>
        /// 库位/机台点位主数据（从 StationPoint/LocationMap 的 MasterValues 中提取）
        /// </summary>
        Master = 3,

        /// <summary>
        /// 执行程段/动作标志（例如 Fetch=2 取料、Put=1 放料）
        /// </summary>
        Leg = 4,

        /// <summary>
        /// 物理端口/槽位（从工位端口字符串解析数值）
        /// </summary>
        Port = 5
    }
}

