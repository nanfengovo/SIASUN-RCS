using System.Diagnostics.CodeAnalysis;

using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            var configuration = context.Services.GetConfiguration();

            // 配置 SQLite 数据库连接字符串，默认地址 Logs/api_audit_log.db（与 Serilog 文本日志在同一 Logs 目录下）
            var rawPath = configuration["AuditLog:SqlitePath"];
            var dbPath = string.IsNullOrWhiteSpace(rawPath) ? "Logs/api_audit_log.db" : rawPath;

            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            context.Services.AddDbContext<AuditLogSqliteDbContext>(options =>
            {
                options.UseSqlite($"Data Source={dbPath};Cache=Shared;");
            });

            // 注册存储接口实现
            context.Services.AddSingleton<IApiAuditLogStore, SqliteApiAuditLogStore>();
            context.Services.AddSingleton<IEntityAuditLogStore, SqliteEntityAuditLogStore>();
        }

        public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            // 自动建标并开启 SQLite WAL（预习日志） 模式（工控机高并发防锁表核心设置）
            using var scope = context.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuditLogSqliteDbContext>();
            await db.Database.EnsureCreatedAsync();
            await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");

            // 注册 Quartz 定时清理任务
            await context.AddBackgroundWorkerAsync<ApiAuditLogCleanupWorker>();
        }
    }
}