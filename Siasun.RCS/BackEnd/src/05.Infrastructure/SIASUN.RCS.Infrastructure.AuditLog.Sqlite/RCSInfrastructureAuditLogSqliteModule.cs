using System.Diagnostics.CodeAnalysis;

using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Auditing;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Volo.Abp.Modularity;
namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    [DependsOn(
    typeof(RCSDomainModule),
    typeof(AbpEntityFrameworkCoreSqliteModule)
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
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            // 自动建标并开启 SQLite WAL（预习日志） 模式（工控机高并发防锁表核心设置）
            using var scope = context.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuditLogSqliteDbContext>();
            db.Database.EnsureCreated();
            db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");
        }
    }
}