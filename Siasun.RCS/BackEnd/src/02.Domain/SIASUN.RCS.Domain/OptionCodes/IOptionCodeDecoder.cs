using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 32 位逆向拆解与语义反查器接口
    /// </summary>
    public interface IOptionCodeDecoder : ITransientDependency
    {
        /// <summary>
        /// 将整型字数组逆向解码为包含位图明细与枚举文本的画像结果
        /// </summary>
        /// <param name="schema">使用的 Schema 定义</param>
        /// <param name="words">各 Part 对应的 32 位整型字列表</param>
        /// <returns>解码后的画像结果</returns>
        DecodedOptionCodeResult DecodeWords(
            OptionCodeSchemaDefinition schema,
            IReadOnlyList<uint> words);

        /// <summary>
        /// 将 OptionCode 字符串（例如 "33621253,33687299"）逆向解码为画像结果
        /// </summary>
        /// <param name="schema">使用的 Schema 定义</param>
        /// <param name="optionCodeString">逗号分隔的 OptionCode 字符串</param>
        /// <returns>解码后的画像结果</returns>
        DecodedOptionCodeResult Decode(
            OptionCodeSchemaDefinition schema,
            string optionCodeString);
    }
}

