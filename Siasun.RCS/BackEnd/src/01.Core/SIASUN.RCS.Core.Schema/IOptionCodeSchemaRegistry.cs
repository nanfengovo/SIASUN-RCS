using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode Schema 统一注册与版本检索中心接口
    /// </summary>
    public interface IOptionCodeSchemaRegistry : ISingletonDependency
    {
        /// <summary>
        /// 注册或覆盖一个 Schema 定义
        /// </summary>
        /// <param name="schema">Schema 定义</param>
        void Register(OptionCodeSchemaDefinition schema);

        /// <summary>
        /// 查找指定代号与版本的 Schema（未找到返回 null）
        /// </summary>
        /// <param name="code">Schema 代号（如 "txc_demo"、"erack"）</param>
        /// <param name="version">版本号（若为 null，默认返回最新最高版本）</param>
        /// <returns>Schema 定义或 null</returns>
        OptionCodeSchemaDefinition? Find(string code, int? version = null);

        /// <summary>
        /// 获取指定代号与版本的 Schema（未找到抛出异常）
        /// </summary>
        /// <param name="code">Schema 代号</param>
        /// <param name="version">版本号（若为 null，默认返回最新最高版本）</param>
        /// <returns>Schema 定义</returns>
        OptionCodeSchemaDefinition Get(string code, int? version = null);

        /// <summary>
        /// 获取所有已注册的 Schema 定义列表
        /// </summary>
        /// <returns>所有 Schema 列表</returns>
        IReadOnlyList<OptionCodeSchemaDefinition> GetAll();
    }
}

