using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Settings;

namespace SIASUN.RCS.Monitor
{
    /// <summary>
    /// 系统硬件资源与磁盘水位监控应用服务
    /// </summary>
    [Authorize(RCSPermissions.SystemMonitor.Default)]
    public class SystemMonitorAppService : ApplicationService, ISystemMonitorAppService
    {
        private readonly ISettingProvider _settingProvider;
        private readonly IRepository<OperationLog, Guid>? _operationLogRepository;
        private readonly IRepository<SystemEventLog, Guid>? _systemEventLogRepository;

        /// <summary>
        /// 构造函数注入设置提供者与审计日志仓储
        /// </summary>
        /// <param name="settingProvider">ABP 设置提供者</param>
        /// <param name="operationLogRepository">操作审计日志仓储（可选）</param>
        /// <param name="systemEventLogRepository">系统事件日志仓储（可选）</param>
        public SystemMonitorAppService(
            ISettingProvider settingProvider,
            IRepository<OperationLog, Guid>? operationLogRepository = null,
            IRepository<SystemEventLog, Guid>? systemEventLogRepository = null)
        {
            _settingProvider = settingProvider;
            _operationLogRepository = operationLogRepository;
            _systemEventLogRepository = systemEventLogRepository;
        }

        /// <summary>
        /// 获取系统硬件与磁盘资源实时指标
        /// </summary>
        /// <returns>系统资源指标</returns>
        public async Task<SystemResourceMetricsDto> GetSystemResourcesAsync()
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var driveRoot = Path.GetPathRoot(logDir);
            var drive = new DriveInfo(driveRoot ?? "C:\\");

            var process = Process.GetCurrentProcess();

            var totalSize = drive.TotalSize;
            var freeSpace = drive.AvailableFreeSpace;
            var usedSpace = totalSize - freeSpace;
            var usedPercent = totalSize > 0 ? (int)Math.Round((double)usedSpace / totalSize * 100) : 0;

            return new SystemResourceMetricsDto
            {
                Disk = new DiskMetricsDto
                {
                    DriveName = drive.Name,
                    TotalSizeBytes = totalSize,
                    FreeSizeBytes = freeSpace,
                    UsedSizeBytes = usedSpace,
                    UsedPercentage = usedPercent,
                    IsSelfHealEnabled = await _settingProvider.GetAsync<bool>(RCSMonitorSettings.IsDiskSelfHealEnabled, true),
                    HighWatermark = await _settingProvider.GetAsync<int>(RCSMonitorSettings.DiskHighWatermark, 85),
                    LowWatermark = await _settingProvider.GetAsync<int>(RCSMonitorSettings.DiskLowWatermark, 70)
                },
                Memory = new MemoryMetricsDto
                {
                    WorkingSet64 = process.WorkingSet64
                }
            };
        }

        /// <summary>
        /// 获取长期容量可观测与前瞻告警健康报告（L4 自治观测）
        /// </summary>
        /// <returns>容量健康报告</returns>
        public async Task<CapacityHealthReportDto> GetCapacityHealthAsync()
        {
            var resources = await GetSystemResourcesAsync();
            var usedPercent = resources.Disk.UsedPercentage;
            var highWatermark = resources.Disk.HighWatermark;
            var warnWatermark = Math.Max(50, highWatermark - 10);

            var alerts = new List<string>();

            // 1. 评估磁盘容量健康
            var diskHealth = CapacityHealthLevel.Healthy;
            if (usedPercent >= highWatermark)
            {
                diskHealth = CapacityHealthLevel.Critical;
                alerts.Add($"磁盘当前使用率达到 {usedPercent}%，已超过高水位警戒线 {highWatermark}%，触发强制自愈清理报警！");
            }
            else if (usedPercent >= warnWatermark)
            {
                diskHealth = CapacityHealthLevel.Warning;
                alerts.Add($"磁盘当前使用率达到 {usedPercent}%，接近高水位红线，请关注工控机存储扩容情况。");
            }

            // 2. 统计日志物理目录占用大小
            long logDirSize = 0;
            try
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (Directory.Exists(logDir))
                {
                    var di = new DirectoryInfo(logDir);
                    foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        logDirSize += fi.Length;
                    }
                }
            }
            catch
            {
                // 忽略工控机权限或文件锁定异常
            }

            // 3. 评估数据库持久化日志行数
            long dbRows = 0;
            if (_operationLogRepository != null)
            {
                dbRows += await _operationLogRepository.GetCountAsync();
            }
            if (_systemEventLogRepository != null)
            {
                dbRows += await _systemEventLogRepository.GetCountAsync();
            }

            var dbHealth = CapacityHealthLevel.Healthy;
            if (dbRows > 2_000_000)
            {
                dbHealth = CapacityHealthLevel.Critical;
                alerts.Add($"数据库日志表累计达 {dbRows:N0} 行，超出严重阈值 (2,000,000)，请确保 DatabaseLogRetentionJob 定时清理任务正常运行！");
            }
            else if (dbRows > 500_000)
            {
                dbHealth = CapacityHealthLevel.Warning;
                alerts.Add($"数据库日志表累计达 {dbRows:N0} 行，已进入预警水位 (500,000)，建议关注归档策略。");
            }

            // 4. 综合健康判定
            var overall = (CapacityHealthLevel)Math.Max((int)diskHealth, (int)dbHealth);

            return new CapacityHealthReportDto
            {
                OverallHealth = overall,
                DiskUsagePercentage = usedPercent,
                DiskHealth = diskHealth,
                LogDirectorySizeBytes = logDirSize,
                DatabaseLogTotalRows = dbRows,
                DatabaseLogHealth = dbHealth,
                ActiveAlerts = alerts,
                EvaluatedAt = DateTime.UtcNow
            };
        }
    }
}
