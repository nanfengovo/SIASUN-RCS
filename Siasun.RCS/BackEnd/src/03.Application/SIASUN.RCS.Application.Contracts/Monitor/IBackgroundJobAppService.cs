using System.Collections.Generic;
using System.Threading.Tasks;
using SIASUN.RCS.BackgroundJobs;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Monitor
{
    /// <summary>
    /// 后台任务调度管理 Application Service 接口
    /// ABP 会自动映射为 RESTful HTTP API：/api/app/background-job/...
    /// </summary>
    public interface IBackgroundJobAppService : IApplicationService
    {
        /// <summary>GET /api/app/background-job — 获取所有 Job 状态</summary>
        Task<List<BackgroundJobDto>> GetAllAsync();

        /// <summary>POST /api/app/background-job/{jobName}/pause</summary>
        Task PauseAsync(string jobName, string groupName);

        /// <summary>POST /api/app/background-job/{jobName}/resume</summary>
        Task ResumeAsync(string jobName, string groupName);

        /// <summary>POST /api/app/background-job/{jobName}/trigger</summary>
        Task TriggerNowAsync(string jobName, string groupName);

        /// <summary>PUT /api/app/background-job/{jobName}/cron</summary>
        Task UpdateCronAsync(string jobName, string groupName, string newCron);
    }
}

