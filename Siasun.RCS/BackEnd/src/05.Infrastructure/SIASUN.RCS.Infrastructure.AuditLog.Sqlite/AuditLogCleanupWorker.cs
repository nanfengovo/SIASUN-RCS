using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    public class AuditLogCleanupWorker : QuartzBackgroundWorkerBase
    {
        public AuditLogCleanupWorker()
        {
            JobDetail = JobBuilder.Create<AuditLogCleanupWorker>()
                .WithIdentity(nameof(AuditLogCleanupWorker), "MaintenanceGroup")
                .WithDescription("每天定期清理过期的 SQLite 审计日志文件")
                .Build();

            Trigger = TriggerBuilder.Create()
                    .WithIdentity($"{nameof(AuditLogCleanupWorker)}Trigger","MaintenanceGroup")
                    .WithSimpleSchedule(s => s.WithIntervalInHours(24).RepeatForever())
                    .Build();
        }

        public override Task Execute(IJobExecutionContext context)
        {
            var configuration = LazyServiceProvider.LazyGetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var retainDays = configuration.GetValue<int>("AuditLog:RetainDays", 30);
            var thresholdDate = DateTime.UtcNow.AddDays(-retainDays);
            var thresholdYm = thresholdDate.ToString("yyyyMM");
            
            var logger = LazyServiceProvider.LazyGetRequiredService<ILogger<AuditLogCleanupWorker>>();
            logger.LogInformation($"开始执行 SQLite 日志文件清理作业... 将清理所属月份小于或等于 {thresholdYm} (由于当月可能有数据，实际逻辑是检查月份是否确实完全早于保留期)。由于我们按月切分，这里删除年月数值小于等于阈值的旧文件。");
            
            try
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (Directory.Exists(logDir))
                {
                    var regex = new Regex(@"api_audit_log_(\d{6})\.db");
                    var files = Directory.GetFiles(logDir, "api_audit_log_*.db*"); // 包含 .db, .db-shm, .db-wal
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        var match = regex.Match(fileInfo.Name);
                        if (match.Success)
                        {
                            var yyyyMM = match.Groups[1].Value;
                            if (string.Compare(yyyyMM, thresholdYm, StringComparison.Ordinal) < 0)
                            {
                                logger.LogInformation($"正在删除过期的日志文件: {fileInfo.Name}");
                                fileInfo.Delete();
                            }
                        }
                        else if (fileInfo.Name.Contains(".db-shm") || fileInfo.Name.Contains(".db-wal"))
                        {
                            // 对于 WAL 和 SHM 文件，尝试解析主数据库名
                            var mainDbName = fileInfo.Name.Replace(".db-wal", ".db").Replace(".db-shm", ".db");
                            var mainMatch = regex.Match(mainDbName);
                            if (mainMatch.Success)
                            {
                                var yyyyMM = mainMatch.Groups[1].Value;
                                if (string.Compare(yyyyMM, thresholdYm, StringComparison.Ordinal) < 0)
                                {
                                    logger.LogInformation($"正在删除过期的日志文件附带项: {fileInfo.Name}");
                                    fileInfo.Delete();
                                }
                            }
                        }
                    }
                }
                
                logger.LogInformation("SQLite 日志清理作业完成！");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "执行 SQLite 日志清理作业时发生异常！");
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
