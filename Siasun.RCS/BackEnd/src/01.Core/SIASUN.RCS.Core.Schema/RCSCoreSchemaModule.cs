using System.Diagnostics.CodeAnalysis;
using Volo.Abp.Modularity;

namespace SIASUN.RCS
{
    /// <summary>
    /// SIASUN RCS 纯算力工具与位图编译器核心模块 (SIASUN.RCS.Core.Schema)
    /// 承载 JSON Schema 解析器、32 位二进制拼装/逆向反解算法、字段编码器（LSB/MSB）
    /// 严禁依赖数据库或业务实体
    /// </summary>
    [DependsOn(typeof(RCSDomainSharedModule))]
    [ExcludeFromCodeCoverage]
    public class RCSCoreSchemaModule : AbpModule
    {
    }
}
