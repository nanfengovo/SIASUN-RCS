using System.Collections.Generic;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 组成部分定义（通常由 1 到多个 32 位整型字组成，例如 codeA / codeB）
    /// </summary>
    public class OptionCodePartDefinition
    {
        /// <summary>
        /// Part 唯一键（例如 "codeA"、"codeB" 或 "TaskCode1"）
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Part 显示标签（例如 "车载动作主指令 (CodeA)"）
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 该 Part 所占用的总位宽（默认 32 位）
        /// </summary>
        public int Width { get; set; } = 32;

        /// <summary>
        /// 该 Part 内部包含的所有位字段定义列表
        /// </summary>
        public List<OptionCodeFieldDefinition> Fields { get; set; } = new();

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public OptionCodePartDefinition()
        {
        }

        /// <summary>
        /// 指定参数构造函数
        /// </summary>
        /// <param name="key">Part 键名</param>
        /// <param name="label">Part 显示名称</param>
        /// <param name="width">位宽（默认 32）</param>
        public OptionCodePartDefinition(string key, string label, int width = 32)
        {
            Key = key;
            Label = label;
            Width = width;
        }
    }
}

