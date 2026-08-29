using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using SIASUN.RCS.Auditing;
using Volo.Abp.BackgroundWorkers.Quartz;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    public class ApiAuditLogCleanupWorker : QuartzBackgroundWorkerBase
    {

        public ApiAuditLogCleanupWorker()
        {
            JobDetail = JobBuilder.Create<ApiAuditLogCleanupWorker>()
                .WithIdentity(nameof(ApiAuditLogCleanupWorker), "MaintenanceGroup")
                .WithDescription("每天定期清理过期的 SQLite API 审计日志")
                .Build();

            Trigger = TriggerBuilder.Create()
                    .WithIdentity($"{nameof(ApiAuditLogCleanupWorker)}Trigger","MaintenanceGroup")
                    .WithSimpleSchedule(s => s.WithIntervalInHours(24).RepeatForever())
                    .Build();
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            // 注意：因为是单例任务调度，请使用 LazyServiceProvider 动态获取服务
            var store = LazyServiceProvider.LazyGetRequiredService<IApiAuditLogStore>();
            
            // 1. 动态获取配置服务
            var configuration = LazyServiceProvider.LazyGetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            
            // 2. 读取 appsettings.json 中的保留天数 (如果没配，默认给 30 天)
            var retainDays = configuration.GetValue<int>("AuditLog:RetainDays", 30);
            
            // 3. 计算出“该杀”的临界时间线
            var threshold = System.DateTime.Now.AddDays(-retainDays);
            
            Logger.LogInformation($"开始执行 SQLite 日志清理作业... 将清理 {threshold:yyyy-MM-dd HH:mm:ss} 之前的日志。");
            
            try
            {
                // 4. 将 Quartz 上下文的 CancellationToken 传给底层，支持优雅停机
                await store.PurgeBeforeAsync(threshold, context.CancellationToken);
                
                Logger.LogInformation("SQLite 日志清理作业完成！");
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "执行 SQLite 日志清理作业时发生异常！");
                
                // 将异常向上抛出，Quartz 会自动捕获并把这次任务标记为“失败 (Failed)”
                throw;
            }
        }
    }
}