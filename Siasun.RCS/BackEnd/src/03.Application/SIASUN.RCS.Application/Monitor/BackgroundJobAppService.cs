using System.Collections.Generic;
using System.Threading.Tasks;
using SIASUN.RCS.BackgroundJobs;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Monitor
{
    public class BackgroundJobAppService : ApplicationService, IBackgroundJobAppService
    {
        private readonly IBackgroundJobService _jobService;

        public BackgroundJobAppService(IBackgroundJobService jobService)
        {
            _jobService = jobService;
        }

        public Task<List<BackgroundJobDto>> GetAllAsync()
            => _jobService.GetAllJobsAsync();

        public Task PauseAsync(string jobName, string groupName)
            => _jobService.PauseJobAsync(jobName, groupName);

        public Task ResumeAsync(string jobName, string groupName)
            => _jobService.ResumeJobAsync(jobName, groupName);

        public Task TriggerNowAsync(string jobName, string groupName)
            => _jobService.TriggerJobNowAsync(jobName, groupName);

        public Task UpdateCronAsync(string jobName, string groupName, string newCron)
        {
            if (string.IsNullOrWhiteSpace(newCron))
            {
                throw new UserFriendlyException("Cron 表达式不能为空");
            }

            return _jobService.UpdateCronAsync(jobName, groupName, newCron);
        }
    }
}

