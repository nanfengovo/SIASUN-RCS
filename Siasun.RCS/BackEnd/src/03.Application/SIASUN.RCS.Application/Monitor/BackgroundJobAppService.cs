using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.BackgroundJobs;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Monitor
{
    /// <summary>
    /// 后台定时任务监控与运维管理应用服务
    /// </summary>
    [Authorize(RCSPermissions.BackgroundJobs.Default)]
    public class BackgroundJobAppService : ApplicationService, IBackgroundJobAppService
    {
        private readonly IBackgroundJobService _jobService;
        private readonly IOperationLogRecorder _operationLogRecorder;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="jobService">底层 Quartz 作业服务</param>
        /// <param name="operationLogRecorder">操作审计记录器</param>
        public BackgroundJobAppService(IBackgroundJobService jobService, IOperationLogRecorder operationLogRecorder)
        {
            _jobService = jobService;
            _operationLogRecorder = operationLogRecorder;
        }

        /// <summary>
        /// 获取所有后台定时任务状态监控列表
        /// </summary>
        public Task<List<BackgroundJobDto>> GetAllAsync()
            => _jobService.GetAllJobsAsync();

        /// <summary>
        /// 暂停指定的后台任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="groupName">任务分组</param>
        [Authorize(RCSPermissions.BackgroundJobs.Manage)]
        [OperationLog(Module = "BackgroundJob", Action = "Pause", TargetType = "BackgroundJob", Description = "调度员手动暂停后台任务")]
        public async Task PauseAsync(string jobName, string groupName)
        {
            await _jobService.PauseJobAsync(jobName, groupName);

            _operationLogRecorder.Record(new OperationLogContext
            {
                Module = "BackgroundJob",
                Action = "Pause",
                TargetType = "BackgroundJob",
                TargetId = $"{groupName}.{jobName}",
                BeforeState = "Running",
                AfterState = "Paused",
                Reason = $"调度员手动暂停后台任务 [{groupName}.{jobName}]"
            });
        }

        /// <summary>
        /// 恢复指定的后台任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="groupName">任务分组</param>
        [Authorize(RCSPermissions.BackgroundJobs.Manage)]
        [OperationLog(Module = "BackgroundJob", Action = "Resume", TargetType = "BackgroundJob", Description = "调度员手动恢复后台任务")]
        public async Task ResumeAsync(string jobName, string groupName)
        {
            await _jobService.ResumeJobAsync(jobName, groupName);

            _operationLogRecorder.Record(new OperationLogContext
            {
                Module = "BackgroundJob",
                Action = "Resume",
                TargetType = "BackgroundJob",
                TargetId = $"{groupName}.{jobName}",
                BeforeState = "Paused",
                AfterState = "Running",
                Reason = $"调度员手动恢复后台任务 [{groupName}.{jobName}]"
            });
        }

        /// <summary>
        /// 立即触发一次后台任务
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="groupName">任务分组</param>
        [Authorize(RCSPermissions.BackgroundJobs.Manage)]
        [OperationLog(Module = "BackgroundJob", Action = "TriggerNow", TargetType = "BackgroundJob", Description = "调度员手动单次立即触发后台任务")]
        public async Task TriggerNowAsync(string jobName, string groupName)
        {
            await _jobService.TriggerJobNowAsync(jobName, groupName);

            _operationLogRecorder.Record(new OperationLogContext
            {
                Module = "BackgroundJob",
                Action = "TriggerNow",
                TargetType = "BackgroundJob",
                TargetId = $"{groupName}.{jobName}",
                BeforeState = "Scheduled",
                AfterState = "Triggered",
                Reason = $"调度员手动单次立即触发后台任务 [{groupName}.{jobName}]"
            });
        }

        /// <summary>
        /// 动态修改指定任务的 Cron 触发表达式
        /// </summary>
        /// <param name="jobName">任务名称</param>
        /// <param name="groupName">任务分组</param>
        /// <param name="newCron">新 Cron 表达式</param>
        [Authorize(RCSPermissions.BackgroundJobs.Manage)]
        [OperationLog(Module = "BackgroundJob", Action = "UpdateCron", TargetType = "BackgroundJob", Description = "调度员更新后台任务 Cron 表达式")]
        public async Task UpdateCronAsync(string jobName, string groupName, string newCron)
        {
            if (string.IsNullOrWhiteSpace(newCron))
            {
                throw new UserFriendlyException("Cron 表达式不能为空");
            }

            await _jobService.UpdateCronAsync(jobName, groupName, newCron);

            _operationLogRecorder.Record(new OperationLogContext
            {
                Module = "BackgroundJob",
                Action = "UpdateCron",
                TargetType = "BackgroundJob",
                TargetId = $"{groupName}.{jobName}",
                BeforeState = "DynamicCron",
                AfterState = newCron,
                Reason = $"调度员更新后台任务 [{groupName}.{jobName}] Cron 表达式为 [{newCron}]"
            });
        }
    }
}

