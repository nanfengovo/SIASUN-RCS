using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 黑匣子全时序事故取证排障应用服务契约
    /// </summary>
    public interface IFlightPackAppService : IApplicationService
    {
        /// <summary>
        /// 导出事故排障黑匣子数据包 (.rcspack / zip)
        /// </summary>
        /// <param name="input">排障导出请求参数</param>
        /// <returns>包含元数据、三轨时序事件、诊断报告及原始日志的压缩包字节流</returns>
        Task<byte[]> ExportAsync(ExportFlightPackDto input);
    }
}
