using System;
using System.Collections.Generic;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 单个位字段定义规范
    /// </summary>
    public class OptionCodeFieldDefinition
    {
        /// <summary>
        /// 字段唯一标识 Key（如 "armSide"、"boxType"、"equipmentType"）
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 字段中文显示名称（如 "机械臂运行侧"、"料盒类型"）
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 起始位（1-based，如 1 对应第 0 位偏移）
        /// </summary>
        public int BitStart { get; set; }

        /// <summary>
        /// 结束位（1-based，包含该位）
        /// </summary>
        public int BitEnd { get; set; }

        /// <summary>
        /// 是否为必须字段（若为 true 且解析结果为空则抛出异常）
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// 字段数据来源类型（Const / Args / Master / Leg / Port）
        /// </summary>
        public OptionCodeFieldSource Source { get; set; } = OptionCodeFieldSource.Args;

        /// <summary>
        /// 固定常量值（仅在 Source 为 Const 时生效）
        /// </summary>
        public long? ConstValue { get; set; }

        /// <summary>
        /// 缺省回退默认值（当可选入参未提供时使用）
        /// </summary>
        public long? DefaultValue { get; set; }

        /// <summary>
        /// 枚举映射字典（数值字符串 -> 业务中文描述，例如 {"1": "左侧", "2": "右侧"}）
        /// </summary>
        public Dictionary<string, string>? Enum { get; set; }

        /// <summary>
        /// 字段补充说明描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 计算字段所占的二进制位数（Width）
        /// </summary>
        public int BitWidth => Math.Max(0, BitEnd - BitStart + 1);

        /// <summary>
        /// 计算字段在 32 位字中的左移偏移量（Shift）
        /// </summary>
        public int Shift => Math.Max(0, BitStart - 1);

        /// <summary>
        /// 计算字段对应的位掩码（Mask）
        /// </summary>
        public uint Mask => BitWidth >= 32 ? 0xFFFFFFFFu : ((1u << BitWidth) - 1u);
    }
}

