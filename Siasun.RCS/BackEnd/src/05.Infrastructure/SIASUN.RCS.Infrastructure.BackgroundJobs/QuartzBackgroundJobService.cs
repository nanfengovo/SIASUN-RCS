using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using SIASUN.RCS.BackgroundJobs;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// IBackgroundJobService 的 Quartz 适配实现
    /// 通过 ISchedulerFactory 查询和操控 Quartz Scheduler，对上层完全屏蔽 Quartz 细节
    /// </summary>
    public class QuartzBackgroundJobService : IBackgroundJobService, ISingletonDependency
    {
        private readonly ISchedulerFactory _schedulerFactory;

        public QuartzBackgroundJobService(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }

        public async Task<List<BackgroundJobDto>> GetAllJobsAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobGroupNames = await scheduler.GetJobGroupNames();
            var result = new List<BackgroundJobDto>();

            foreach (var groupName in jobGroupNames)
            {
                var groupMatcher = Quartz.Impl.Matchers.GroupMatcher<JobKey>.GroupEquals(groupName);
                var jobKeys = await scheduler.GetJobKeys(groupMatcher);

                foreach (var jobKey in jobKeys)
                {
                    var jobDetail = await scheduler.GetJobDetail(jobKey);
                    var triggers = (await scheduler.GetTriggersOfJob(jobKey)).ToList();

                    var firstTrigger = triggers.FirstOrDefault();
                    var triggerState = firstTrigger is not null
                        ? await scheduler.GetTriggerState(firstTrigger.Key)
                        : TriggerState.None;

                    var cronExpression = triggers.OfType<ICronTrigger>().FirstOrDefault()?.CronExpressionString;
                    var nextFireTime = firstTrigger?.GetNextFireTimeUtc()?.UtcDateTime;
                    var prevFireTime = firstTrigger?.GetPreviousFireTimeUtc()?.UtcDateTime;

                    result.Add(new BackgroundJobDto
                    {
                        JobName = jobKey.Name,
                        GroupName = jobKey.Group,
                        Description = jobDetail?.Description ?? string.Empty,
                        State = triggerState.ToString(),
                        NextFireTime = nextFireTime,
                        PreviousFireTime = prevFireTime,
                        CronExpression = cronExpression
                    });
                }
            }

            return result;
        }

        public async Task PauseJobAsync(string jobName, string groupName)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.PauseJob(new JobKey(jobName, groupName));
        }

        public async Task ResumeJobAsync(string jobName, string groupName)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.ResumeJob(new JobKey(jobName, groupName));
        }

        public async Task TriggerJobNowAsync(string jobName, string groupName)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            await scheduler.TriggerJob(new JobKey(jobName, groupName));
        }

        public async Task UpdateCronAsync(string jobName, string groupName, string newCron)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName, groupName);
            var triggers = (await scheduler.GetTriggersOfJob(jobKey)).OfType<ICronTrigger>().ToList();

            foreach (var trigger in triggers)
            {
                var newTrigger = TriggerBuilder.Create()
                    .WithIdentity(trigger.Key)
                    .ForJob(jobKey)
                    .WithCronSchedule(newCron)
                    .Build();

                await scheduler.RescheduleJob(trigger.Key, newTrigger);
            }
        }
    }
}

