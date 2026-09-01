using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    /// <summary>
    /// 纯业务逻辑服务：清理过期的 SQLite 审计日志文件
    /// 不依赖任何调度框架，可被任意触发方式调用（Quartz、定时器、API 等）
    /// </summary>
    public class AuditLogCleanupService : ITransientDependency
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuditLogCleanupService> _logger;

        public AuditLogCleanupService(IConfiguration configuration, ILogger<AuditLogCleanupService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task CleanExpiredFilesAsync(CancellationToken cancellationToken = default)
        {
            var retainDays = _configuration.GetValue<int>("AuditLog:RetainDays", 30);
            var thresholdDate = DateTime.UtcNow.AddDays(-retainDays);
            var thresholdYm = thresholdDate.ToString("yyyyMM");

            _logger.LogInformation("开始执行 SQLite 日志文件清理，将删除年月早于 {ThresholdYm} 的文件", thresholdYm);

            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                _logger.LogInformation("日志目录 {LogDir} 不存在，跳过清理", logDir);
                return Task.CompletedTask;
            }

            var regex = new Regex(@"api_audit_log_(\d{6})\.db");
            var files = Directory.GetFiles(logDir, "api_audit_log_*.db*"); // 包含 .db, .db-shm, .db-wal

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(file);
                var yyyyMM = ResolveYearMonth(fileInfo.Name, regex);

                if (yyyyMM is not null && string.Compare(yyyyMM, thresholdYm, StringComparison.Ordinal) < 0)
                {
                    try
                    {
                        _logger.LogInformation("正在删除过期日志文件: {FileName}", fileInfo.Name);
                        fileInfo.Delete();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "删除日志文件 {FileName} 时发生异常，跳过", fileInfo.Name);
                    }
                }
            }

            _logger.LogInformation("SQLite 日志清理完成");
            return Task.CompletedTask;
        }

        private static string? ResolveYearMonth(string fileName, Regex regex)
        {
            // 直接匹配 .db 文件
            var match = regex.Match(fileName);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // WAL / SHM 附件，推断主文件名
            var mainDbName = fileName.Replace(".db-wal", ".db").Replace(".db-shm", ".db");
            var mainMatch = regex.Match(mainDbName);
            return mainMatch.Success ? mainMatch.Groups[1].Value : null;
        }
    }
}

