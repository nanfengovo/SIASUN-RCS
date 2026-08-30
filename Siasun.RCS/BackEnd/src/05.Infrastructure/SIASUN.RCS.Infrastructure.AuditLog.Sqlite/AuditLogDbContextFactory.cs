using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    public class AuditLogDbContextFactory : IAuditLogDbContextFactory, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, bool> _initializedDbs = new();

        public async Task<AuditLogSqliteDbContext> CreateAsync(DateTime? time = null)
        {
            var targetTime = time ?? DateTime.UtcNow;
            var dbFileName = $"api_audit_log_{targetTime:yyyyMM}.db";
            
            // 确保存放目录存在
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var dbPath = Path.Combine(logDir, dbFileName);
            var connectionString = $"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared";

            var optionsBuilder = new DbContextOptionsBuilder<AuditLogSqliteDbContext>();
            optionsBuilder.UseSqlite(connectionString);
            
            var dbContext = new AuditLogSqliteDbContext(optionsBuilder.Options);

            // 如果本月第一次访问，确保建表
            if (!_initializedDbs.ContainsKey(dbPath))
            {
                await dbContext.Database.EnsureCreatedAsync();
                
                // 开启 WAL 模式提高并发性能
                await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                
                _initializedDbs.TryAdd(dbPath, true);
            }

            return dbContext;
        }
    }
}
