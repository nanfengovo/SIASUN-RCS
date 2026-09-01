using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIASUN.RCS.BackgroundJobs
{
    /// <summary>
    /// 后台任务调度管理服务接口（Domain 层，无调度框架依赖）
    /// 由基础设施层的 QuartzBackgroundJobService 实现
    /// </summary>
    public interface IBackgroundJobService
    {
        /// <summary>获取所有已注册 Job 的当前状态</summary>
        Task<List<BackgroundJobDto>> GetAllJobsAsync();

        /// <summary>暂停 Job</summary>
        Task PauseJobAsync(string jobName, string groupName);

        /// <summary>恢复已暂停的 Job</summary>
        Task ResumeJobAsync(string jobName, string groupName);

        /// <summary>立即触发一次 Job（不影响既有调度）</summary>
        Task TriggerJobNowAsync(string jobName, string groupName);

        /// <summary>更新 Job 的 Cron 表达式（当次运行有效；持久化 Store 下重启仍生效）</summary>
        Task UpdateCronAsync(string jobName, string groupName, string newCron);
    }
}

