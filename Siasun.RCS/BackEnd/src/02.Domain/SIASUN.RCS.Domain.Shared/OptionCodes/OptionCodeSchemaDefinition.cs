using System.Collections.Generic;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 完整 Schema 结构契约定义
    /// </summary>
    public class OptionCodeSchemaDefinition
    {
        /// <summary>
        /// Schema 业务代号（例如 "txc_demo"、"erack"、"molding"）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Schema 版本号（从 1 开始递增）
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Schema 显示标题（例如 "台湾晶技 TXC 标准治具 Schema"）
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 业务背景与规范描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 协议线格式配置
        /// </summary>
        public OptionCodeWireFormat Wire { get; set; } = new();

        /// <summary>
        /// 该 Schema 下属的所有 Part 组成部分
        /// </summary>
        public List<OptionCodePartDefinition> Parts { get; set; } = new();

        /// <summary>
        /// 完整版本化唯一键（例如 "txc_demo.v1"、"molding.v1"）
        /// </summary>
        public string FullKey => $"{Code}.v{Version}".ToLowerInvariant();

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public OptionCodeSchemaDefinition()
        {
        }

        /// <summary>
        /// 指定基本信息构造函数
        /// </summary>
        /// <param name="code">Schema 标识代号</param>
        /// <param name="version">版本号</param>
        /// <param name="title">标题</param>
        /// <param name="description">描述说明</param>
        public OptionCodeSchemaDefinition(string code, int version, string title, string? description = null)
        {
            Code = code;
            Version = version;
            Title = title;
            Description = description;
        }
    }
}

