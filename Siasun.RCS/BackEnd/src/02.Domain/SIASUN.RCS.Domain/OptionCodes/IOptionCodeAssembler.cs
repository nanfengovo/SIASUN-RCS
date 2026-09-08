using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 字段多源装配器接口
    /// </summary>
    public interface IOptionCodeAssembler : ITransientDependency
    {
        /// <summary>
        /// 根据 Schema 定义与上下文多源数据，装配并校验每个 Part 下各个位字段的无符号整数值
        /// </summary>
        /// <param name="schema">Schema 定义</param>
        /// <param name="context">装配上下文（任务参数、主数据、程段、端口等）</param>
        /// <returns>每个 Part 对应的字段无符号数值字典 [PartKey -> [FieldKey -> Value]]</returns>
        Dictionary<string, Dictionary<string, uint>> Assemble(
            OptionCodeSchemaDefinition schema,
            OptionCodeAssembleContext context);
    }
}

