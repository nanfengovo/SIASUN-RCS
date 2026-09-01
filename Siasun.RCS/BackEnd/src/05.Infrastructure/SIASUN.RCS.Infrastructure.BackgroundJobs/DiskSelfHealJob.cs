using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using SIASUN.RCS.Infrastructure.AuditLog.Sqlite;
using SIASUN.RCS.Monitor;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Settings;

namespace SIASUN.RCS.Infrastructure.BackgroundJobs
{
    [DisallowConcurrentExecution]
    public class DiskSelfHealJob : IJob
    {
        private readonly ISettingProvider _settingProvider;
        private readonly AuditLogCleanupService _cleanupService;
        private readonly IRepository<SystemEventLog, Guid> _eventLogRepository;
        private readonly ILogger<DiskSelfHealJob> _logger;

        public DiskSelfHealJob(
            ISettingProvider settingProvider,
            AuditLogCleanupService cleanupService,
            IRepository<SystemEventLog, Guid> eventLogRepository,
            ILogger<DiskSelfHealJob> logger)
        {
            _settingProvider = settingProvider;
            _cleanupService = cleanupService;
            _eventLogRepository = eventLogRepository;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var isEnabled = await _settingProvider.GetAsync<bool>(RCSMonitorSettings.IsDiskSelfHealEnabled, true);
            if (!isEnabled)
            {
                return;
            }

            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                return;
            }

            var drive = new DriveInfo(Path.GetPathRoot(logDir)!);
            if (!drive.IsReady)
            {
                return;
            }

            // 计算当前使用率 (%)
            var totalSize = drive.TotalSize;
            var freeSpace = drive.AvailableFreeSpace;
            var usedSpace = totalSize - freeSpace;
            var usedPercent = (int)Math.Round((double)usedSpace / totalSize * 100);

            var highWatermark = await _settingProvider.GetAsync<int>(RCSMonitorSettings.DiskHighWatermark, 85);
            var lowWatermark = await _settingProvider.GetAsync<int>(RCSMonitorSettings.DiskLowWatermark, 70);
            var hardRetentionHours = await _settingProvider.GetAsync<int>(RCSMonitorSettings.HardRetentionHours, 0);

            if (usedPercent >= highWatermark)
            {
                _logger.LogWarning("磁盘容量告警！当前使用率 {UsedPercent}%，触发高水位线 {HighWatermark}%。进入紧急自愈清理模式。", usedPercent, highWatermark);

                // 在真实系统中，这里可以通过 SignalR 发送横幅报警
                // _alertNotifier.BroadcastWarning($"磁盘空间不足 ({usedPercent}%)，系统正在紧急清理历史日志。");

                // 计算要清理到的目标剩余空间 (字节)
                var targetUsedSpaceBytes = (long)(totalSize * (lowWatermark / 100.0));
                var targetFreeSpaceBytes = totalSize - targetUsedSpaceBytes;

                var watch = System.Diagnostics.Stopwatch.StartNew();

                // 执行基于容量的清理
                var deletedFiles = await _cleanupService.CleanByCapacityAsync(targetFreeSpaceBytes, hardRetentionHours, context.CancellationToken);

                watch.Stop();

                // 重新获取磁盘状态
                drive = new DriveInfo(Path.GetPathRoot(logDir)!);
                var postFreeSpace = drive.AvailableFreeSpace;
                var postUsedPercent = (int)Math.Round((double)(totalSize - postFreeSpace) / totalSize * 100);
                var releasedBytes = postFreeSpace - freeSpace;

                var logMessage = $"触发高水位清理。磁盘由 {usedPercent}% 降至 {postUsedPercent}%。释放空间 {releasedBytes / 1024 / 1024} MB。";

                _logger.LogInformation(logMessage);

                // 记录自愈追溯日志到数据库
                await _eventLogRepository.InsertAsync(new SystemEventLog(
                    Guid.NewGuid(),
                    "DiskSelfHeal",
                    "Warning",
                    logMessage,
                    JsonSerializer.Serialize(new
                    {
                        PreUsagePercent = usedPercent,
                        PostUsagePercent = postUsedPercent,
                        HighWatermark = highWatermark,
                        LowWatermark = lowWatermark,
                        DeletedFileCount = deletedFiles.Count,
                        DeletedFiles = deletedFiles,
                        ElapsedMilliseconds = watch.ElapsedMilliseconds
                    })
                ));
            }
        }
    }
}

