using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Auditing;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    [DependsOn(
    typeof(RCSDomainModule),
    typeof(AbpEntityFrameworkCoreSqliteModule),
    typeof(AbpBackgroundWorkersQuartzModule)
    )]
    [ExcludeFromCodeCoverage]
    public class RCSInfrastructureAuditLogSqliteModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 存储接口实现和按月分库工厂已通过 ISingletonDependency 自动注册
        }

        public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            // 初始化本月对应的数据库
            var factory = context.ServiceProvider.GetRequiredService<IAuditLogDbContextFactory>();
            await factory.CreateAsync();

            // 注册 Quartz 定时清理任务
            await context.AddBackgroundWorkerAsync<AuditLogCleanupWorker>();
        }
    }
}
