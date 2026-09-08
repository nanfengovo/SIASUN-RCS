using System.Collections.Generic;

namespace SIASUN.RCS.OptionCodes.Dtos
{
    /// <summary>
    /// OptionCode 动态装配编译结果 DTO
    /// </summary>
    public class CompileOptionCodeResultDto
    {
        /// <summary>
        /// 编译生成的 TM 协议 OptionCode 字符串（例如 "33621253,33687299"）
        /// </summary>
        public string OptionCode { get; set; } = string.Empty;

        /// <summary>
        /// 使用的 Schema 唯一代号
        /// </summary>
        public string SchemaCode { get; set; } = string.Empty;

        /// <summary>
        /// 使用的 Schema 版本号
        /// </summary>
        public int SchemaVersion { get; set; }

        /// <summary>
        /// 各 Part 对应的 32 位无符号整型数值列表
        /// </summary>
        public IReadOnlyList<uint> Words { get; set; } = new List<uint>();

        /// <summary>
        /// 即时反向解析明细画像（供前端调试大屏与运维看板展开）
        /// </summary>
        public DecodedOptionCodeResult DecodedDetail { get; set; } = null!;
    }
}

