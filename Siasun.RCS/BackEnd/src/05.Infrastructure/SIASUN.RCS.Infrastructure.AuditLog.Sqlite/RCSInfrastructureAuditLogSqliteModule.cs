using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Auditing;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    /// <summary>
    /// AuditLog SQLite 存储模块。
    /// 只负责数据访问（DbContext、Store）和 SQLite 按月分库逻辑。
    /// Quartz 调度注册已移至 SIASUN.RCS.Infrastructure.BackgroundJobs 统一管理。
    /// </summary>
    [DependsOn(
        typeof(RCSDomainModule),
        typeof(AbpEntityFrameworkCoreSqliteModule)
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
        }
    }
}
