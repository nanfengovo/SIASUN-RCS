using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 32 位位掩码正向编译器接口
    /// </summary>
    public interface IOptionCodeEncoder : ITransientDependency
    {
        /// <summary>
        /// 将已装配的各 Part 字段数值列表编译为 32 位整型字数组
        /// </summary>
        /// <param name="schema">Schema 结构定义</param>
        /// <param name="assembledValues">已装配字段数值字典 [PartKey -> [FieldKey -> Value]]</param>
        /// <returns>每个 Part 对应的 32 位无符号整型字列表</returns>
        IReadOnlyList<uint> EncodeToWords(
            OptionCodeSchemaDefinition schema,
            Dictionary<string, Dictionary<string, uint>> assembledValues);

        /// <summary>
        /// 将已装配的各 Part 字段数值列表直接编译并格式化为 TM 协议线格式字符串（例如 "33621253,33687299"）
        /// </summary>
        /// <param name="schema">Schema 结构定义</param>
        /// <param name="assembledValues">已装配字段数值字典</param>
        /// <returns>TM 协议 OptionCode 字符串</returns>
        string Encode(
            OptionCodeSchemaDefinition schema,
            Dictionary<string, Dictionary<string, uint>> assembledValues);
    }
}

