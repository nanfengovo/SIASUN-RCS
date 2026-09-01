using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SIASUN.RCS.Infrastructure.AuditLog.Sqlite;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// 后台任务调度中台模块。
    /// 职责：
    ///   1. 配置 Quartz（当前使用 RAMJobStore；切换到 AdoJobStore 只需修改此处配置）
    ///   2. 统一注册所有业务 Job 及其默认 Trigger
    ///   3. 向 DI 注册 IBackgroundJobService 的实现（QuartzBackgroundJobService）
    ///
    /// 未来新增 Job 步骤：
    ///   - 在对应业务模块中实现 IJob（不引入 Quartz 模块依赖）
    ///   - 在本模块 ConfigureServices 中调用 AddJobAndTrigger&lt;TJob&gt;
    /// </summary>
    [DependsOn(
        typeof(RCSDomainModule),
        typeof(AbpBackgroundWorkersQuartzModule),
        typeof(RCSInfrastructureAuditLogSqliteModule)
    )]
    [ExcludeFromCodeCoverage]
    public class RCSBackgroundJobsModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            // ── 配置 Quartz（第一期 RAMJobStore）────────────────────────────
            // 切换到持久化时替换此段为 services.AddQuartz(q => q.UsePersistentStore(...)) 即可，业务 Job 代码无需改动
            // ABP 的 AbpBackgroundWorkersQuartzModule 已自动设置 MicrosoftDependencyInjectionJobFactory
            Configure<QuartzOptions>(options =>
            {
                // ─── 统一注册所有 Job 及其默认 Cron ─────────────────────────
                var cleanupCron = configuration.GetValue<string>("AuditLog:CleanupCron", "0 0 2 * * ?");

                options.AddJobAndTrigger<AuditLogCleanupJob>(
                    jobName: nameof(AuditLogCleanupJob),
                    groupName: "Maintenance",
                    description: "每天定期清理过期的 SQLite 审计日志文件",
                    cronExpression: cleanupCron!);

                options.AddJobAndTrigger<DiskSelfHealJob>(
                    jobName: nameof(DiskSelfHealJob),
                    groupName: "Maintenance",
                    description: "高频自卫监控，磁盘达到高水位时强制清理日志",
                    cronExpression: "0 */5 * * * ?"); // 默认每 5 分钟检查一次
            });
        }
    }

    internal static class QuartzOptionsExtensions
    {
        public static void AddJobAndTrigger<TJob>(
            this QuartzOptions options,
            string jobName,
            string groupName,
            string description,
            string cronExpression) where TJob : IJob
        {
            options.AddJob<TJob>(j => j
                .WithIdentity(jobName, groupName)
                .WithDescription(description)
                .StoreDurably());

            options.AddTrigger(t => t
                .ForJob(jobName, groupName)
                .WithIdentity($"{jobName}Trigger", groupName)
                .WithCronSchedule(cronExpression));
        }
    }
}

