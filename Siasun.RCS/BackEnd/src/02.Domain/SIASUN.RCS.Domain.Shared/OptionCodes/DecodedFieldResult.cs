using System;
using System.Collections.Generic;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// 单个位字段反向解析提取结果
    /// </summary>
    public class DecodedFieldResult
    {
        /// <summary>
        /// 字段唯一标识 Key
        /// </summary>
        public string FieldKey { get; set; } = string.Empty;

        /// <summary>
        /// 字段中文标签
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 1-based 起始位
        /// </summary>
        public int BitStart { get; set; }

        /// <summary>
        /// 1-based 结束位
        /// </summary>
        public int BitEnd { get; set; }

        /// <summary>
        /// 占用的二进制位数
        /// </summary>
        public int BitWidth { get; set; }

        /// <summary>
        /// 提取出的原始十进制无符号数值
        /// </summary>
        public uint RawValue { get; set; }

        /// <summary>
        /// 业务展示文本（若定义了枚举则为翻译后的中文，例如 "SP170机台"；否则为数值串）
        /// </summary>
        public string DisplayValue { get; set; } = string.Empty;

        /// <summary>
        /// 格式化后的二进制位串（左侧补0，例如 "00000010"）
        /// </summary>
        public string BinaryString { get; set; } = string.Empty;

        /// <summary>
        /// 数据源来源类型
        /// </summary>
        public OptionCodeFieldSource Source { get; set; }

        /// <summary>
        /// 字段说明描述
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// OptionCode 单个 Part（32位字）反向解析结果
    /// </summary>
    public class DecodedPartResult
    {
        /// <summary>
        /// Part 唯一键（如 "codeA"、"TaskCode1"）
        /// </summary>
        public string PartKey { get; set; } = string.Empty;

        /// <summary>
        /// Part 显示标签
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 总位宽
        /// </summary>
        public int Width { get; set; } = 32;

        /// <summary>
        /// 原始 32 位无符号整型数值
        /// </summary>
        public uint RawWord { get; set; }

        /// <summary>
        /// 十六进制表示字符串（例如 "0x02010005"）
        /// </summary>
        public string HexWord => $"0x{RawWord:X8}";

        /// <summary>
        /// 格式化为按字节分段的 32 位二进制字符串（例如 "00000010 00000001 00000000 00000101"）
        /// </summary>
        public string BinaryWord
        {
            get
            {
                var rawBinary = Convert.ToString(RawWord, 2).PadLeft(Width, '0');
                if (rawBinary.Length == 32)
                {
                    return $"{rawBinary.Substring(0, 8)} {rawBinary.Substring(8, 8)} {rawBinary.Substring(16, 8)} {rawBinary.Substring(24, 8)}";
                }
                return rawBinary;
            }
        }

        /// <summary>
        /// 该 Part 内部解析出的所有位字段列表
        /// </summary>
        public List<DecodedFieldResult> Fields { get; set; } = new();
    }

    /// <summary>
    /// OptionCode 完整逆向解析画像结果
    /// </summary>
    public class DecodedOptionCodeResult
    {
        /// <summary>
        /// 使用的 Schema 业务代号
        /// </summary>
        public string SchemaCode { get; set; } = string.Empty;

        /// <summary>
        /// 使用的 Schema 版本号
        /// </summary>
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Schema 显示标题
        /// </summary>
        public string SchemaTitle { get; set; } = string.Empty;

        /// <summary>
        /// 传入解析的原始 OptionCode 报文字符串（如 "33621253,33687299"）
        /// </summary>
        public string OptionCode { get; set; } = string.Empty;

        /// <summary>
        /// 解析出的各个 Part 结果列表
        /// </summary>
        public List<DecodedPartResult> Parts { get; set; } = new();
    }
}

