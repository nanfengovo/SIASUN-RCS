using System.Collections.Generic;
using System.Threading.Tasks;
using SIASUN.RCS.OptionCodes.Dtos;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 动态位图编译、逆向反解与快照固化应用服务契约
    /// </summary>
    public interface IOptionCodeAppService : IApplicationService
    {
        /// <summary>
        /// 获取所有已注册的 OptionCode Schema 列表（供前端可视化设计器与大屏选择）
        /// </summary>
        /// <returns>Schema 列表</returns>
        Task<IReadOnlyList<OptionCodeSchemaDefinition>> GetSchemasAsync();

        /// <summary>
        /// 获取指定代号与版本的 Schema 详细位字段结构
        /// </summary>
        /// <param name="schemaCode">Schema 代号（如 "txc_demo"、"erack"、"molding"）</param>
        /// <param name="version">版本号（可选，默认最新版本）</param>
        /// <returns>Schema 详情</returns>
        Task<OptionCodeSchemaDefinition> GetSchemaAsync(string schemaCode, int? version = null);

        /// <summary>
        /// 动态装配参数并编译 32 位 OptionCode 字符串（附带即时反向解析明细）
        /// </summary>
        /// <param name="input">编译请求入参</param>
        /// <returns>编译输出与反向解析画像</returns>
        Task<CompileOptionCodeResultDto> CompileAsync(CompileOptionCodeInput input);

        /// <summary>
        /// 对给定的 OptionCode 报文字符串进行逆向反解（拆解为位字段二进制与业务语义描述）
        /// </summary>
        /// <param name="input">反解请求入参</param>
        /// <returns>逆向反解明细画像</returns>
        Task<DecodedOptionCodeResult> DecodeAsync(DecodeOptionCodeInput input);

        /// <summary>
        /// 为指定在途或新建任务编译并固化 OptionCode 快照（写入 AgvTask 聚合根，杜绝主数据漂移）
        /// </summary>
        /// <param name="input">冻结固化请求</param>
        /// <returns>编译并固化的结果</returns>
        Task<CompileOptionCodeResultDto> FreezeTaskOptionCodeAsync(FreezeTaskOptionCodeInput input);
    }
}

