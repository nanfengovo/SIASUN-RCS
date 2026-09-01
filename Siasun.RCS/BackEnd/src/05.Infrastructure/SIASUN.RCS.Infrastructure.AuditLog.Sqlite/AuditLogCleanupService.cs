using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
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

        /// <summary>
        /// 基于容量的自愈清理（紧急抢救）
        /// </summary>
        /// <param name="targetFreeSpaceBytes">需要达到的目标剩余空间大小</param>
        /// <param name="hardRetentionHours">绝对不删的底线时间（防止全盘删空）</param>
        /// <param name="cancellationToken"></param>
        /// <returns>返回本次删除了哪些文件、释放了多少空间等结果（由于不是重点，通过异常中断返回或日志体现）</returns>
        public async Task<System.Collections.Generic.List<string>> CleanByCapacityAsync(long targetFreeSpaceBytes, int hardRetentionHours, CancellationToken cancellationToken = default)
        {
            var deletedFiles = new System.Collections.Generic.List<string>();
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                return deletedFiles;
            }

            var drive = new DriveInfo(Path.GetPathRoot(logDir)!);
            if (drive.AvailableFreeSpace >= targetFreeSpaceBytes)
            {
                return deletedFiles; // 已经满足，无需清理
            }

            var regex = new Regex(@"api_audit_log_(\d{6})\.db");
            // 按最后写入时间升序，最旧的最先删
            var files = new DirectoryInfo(logDir).GetFiles("api_audit_log_*.db*")
                                                 .OrderBy(f => f.LastWriteTimeUtc)
                                                 .ToList();

            var hardLimitTime = hardRetentionHours > 0 
                ? DateTime.UtcNow.AddHours(-hardRetentionHours) 
                : DateTime.MinValue;

            foreach (var fileInfo in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (drive.AvailableFreeSpace >= targetFreeSpaceBytes)
                {
                    break; // 达标即停止
                }

                if (fileInfo.LastWriteTimeUtc >= hardLimitTime)
                {
                    _logger.LogCritical("清理已触及硬保留底线({HardRetentionHours}小时)，无法继续清理。当前仍未达到低水位！", hardRetentionHours);
                    break; // 触及底线，必须停止
                }

                try
                {
                    _logger.LogWarning("容量告急，强制删除日志文件: {FileName}", fileInfo.Name);
                    fileInfo.Delete();
                    deletedFiles.Add(fileInfo.Name);
                    // 删除后休眠一小会儿等待 OS 释放，避免 DriveInfo 读不到最新
                    await Task.Delay(100, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "容量告急清理，删除日志文件 {FileName} 时发生异常，跳过", fileInfo.Name);
                }
            }

            return deletedFiles;
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

