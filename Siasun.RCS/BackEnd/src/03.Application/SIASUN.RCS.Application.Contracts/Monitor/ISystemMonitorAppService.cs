using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Monitor
{
    /// <summary>
    /// 系统硬件资源与容量健康监控应用服务契约
    /// </summary>
    public interface ISystemMonitorAppService : IApplicationService
    {
        /// <summary>
        /// 获取系统硬件与磁盘资源实时指标
        /// </summary>
        /// <returns>系统资源指标</returns>
        Task<SystemResourceMetricsDto> GetSystemResourcesAsync();

        /// <summary>
        /// 获取长期容量可观测与前瞻告警健康报告（L4 自治观测）
        /// </summary>
        /// <returns>容量健康报告</returns>
        Task<CapacityHealthReportDto> GetCapacityHealthAsync();
    }
}
