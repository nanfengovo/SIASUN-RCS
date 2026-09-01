using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Monitor
{
    public interface ISystemMonitorAppService : IApplicationService
    {
        Task<SystemResourceMetricsDto> GetSystemResourcesAsync();
    }
}

