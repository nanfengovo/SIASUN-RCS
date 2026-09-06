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
        private readonly SIASUN.RCS.Auditing.IApiAuditLogChannel? _apiAuditLogChannel;
        private readonly SIASUN.RCS.Auditing.IEntityAuditLogChannel? _entityAuditLogChannel;
        private readonly SIASUN.RCS.Auditing.IOperationLogChannel? _operationLogChannel;
        private readonly SIASUN.RCS.Diagnostics.ILiveStreamTelemetryProvider? _liveStreamTelemetry;
        private readonly SIASUN.RCS.Diagnostics.IAdaptiveTrafficGovernor? _trafficGovernor;

        /// <summary>
        /// 构造函数注入设置提供者、审计日志仓储与诊断通道监控组件
        /// </summary>
        /// <param name="settingProvider">ABP 设置提供者</param>
        /// <param name="operationLogRepository">操作审计日志仓储（可选）</param>
        /// <param name="systemEventLogRepository">系统事件日志仓储（可选）</param>
        /// <param name="apiAuditLogChannel">API 审计日志通道（可选）</param>
        /// <param name="entityAuditLogChannel">实体审计日志通道（可选）</param>
        /// <param name="operationLogChannel">操作审计日志通道（可选）</param>
        /// <param name="liveStreamTelemetry">实时推流遥测提供者（可选）</param>
        /// <param name="trafficGovernor">自适应流量控制器（可选）</param>
        public SystemMonitorAppService(
            ISettingProvider settingProvider,
            IRepository<OperationLog, Guid>? operationLogRepository = null,
            IRepository<SystemEventLog, Guid>? systemEventLogRepository = null,
            SIASUN.RCS.Auditing.IApiAuditLogChannel? apiAuditLogChannel = null,
            SIASUN.RCS.Auditing.IEntityAuditLogChannel? entityAuditLogChannel = null,
            SIASUN.RCS.Auditing.IOperationLogChannel? operationLogChannel = null,
            SIASUN.RCS.Diagnostics.ILiveStreamTelemetryProvider? liveStreamTelemetry = null,
            SIASUN.RCS.Diagnostics.IAdaptiveTrafficGovernor? trafficGovernor = null)
        {
            _settingProvider = settingProvider;
            _operationLogRepository = operationLogRepository;
            _systemEventLogRepository = systemEventLogRepository;
            _apiAuditLogChannel = apiAuditLogChannel;
            _entityAuditLogChannel = entityAuditLogChannel;
            _operationLogChannel = operationLogChannel;
            _liveStreamTelemetry = liveStreamTelemetry;
            _trafficGovernor = trafficGovernor;
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

            // 4. 统计异步审计通道积压与特权溢流保全指标
            var apiDepth = _apiAuditLogChannel?.TotalQueueCount ?? 0;
            var entityDepth = _entityAuditLogChannel?.TotalQueueCount ?? 0;
            var opDepth = _operationLogChannel?.TotalQueueCount ?? 0;
            var liveStreamDepth = _liveStreamTelemetry?.PendingCount ?? 0;

            var apiSpill = _apiAuditLogChannel?.SpillCount ?? 0;
            var entitySpill = _entityAuditLogChannel?.SpillCount ?? 0;
            var opSpill = _operationLogChannel?.SpillCount ?? 0;
            var totalSpill = apiSpill + entitySpill + opSpill;

            var apiPendingSpill = _apiAuditLogChannel?.PendingSpillCount ?? 0;
            var entityPendingSpill = _entityAuditLogChannel?.PendingSpillCount ?? 0;
            var opPendingSpill = _operationLogChannel?.PendingSpillCount ?? 0;
            var totalPendingSpill = apiPendingSpill + entityPendingSpill + opPendingSpill;

            var spillHealth = CapacityHealthLevel.Healthy;
            if (totalPendingSpill > 0)
            {
                spillHealth = CapacityHealthLevel.Critical;
                alerts.Add($"检测到核心审计特权证据正处于应急溢流落盘待消费状态 (当前未消费溢出: {totalPendingSpill:N0} 条, 累计保全: {totalSpill:N0} 条)，请排查工控机写库吞吐与通道负载！");
            }
            else if (totalSpill > 0)
            {
                spillHealth = CapacityHealthLevel.Warning;
                alerts.Add($"核心审计特权证据历史累计触发应急溢流保全 {totalSpill:N0} 条 (当前已全部恢复/入库)，建议关注工控机网络或写库偶发阻塞。");
            }
            else if (apiDepth > 1000 || entityDepth > 1000 || opDepth > 1000)
            {
                alerts.Add($"审计通道内部积压偏高 (API通道: {apiDepth}, 实体通道: {entityDepth}, 操作通道: {opDepth})，请关注后台消费 Worker 处理时效。");
            }

            // 5. 采集自适应流量控制器运行指标
            double currentEps = 0;
            long droppedCount = 0;
            if (_trafficGovernor != null)
            {
                var govMetrics = _trafficGovernor.GetMetrics();
                currentEps = govMetrics.CurrentEps;
                droppedCount = govMetrics.TotalDroppedCount;
                if (govMetrics.CurrentLevel == SIASUN.RCS.Diagnostics.TrafficGovernorLevel.CriticalBurst)
                {
                    alerts.Add($"自适应流量调控器已进入 CriticalBurst 突发削峰状态 (当前 EPS: {currentEps:F1})，低优先级遥测已被强力抑制。");
                }
            }

            // 6. 综合健康判定（包含特权溢流安全等级）
            var overall = (CapacityHealthLevel)Math.Max(Math.Max((int)diskHealth, (int)dbHealth), (int)spillHealth);

            return new CapacityHealthReportDto
            {
                OverallHealth = overall,
                DiskUsagePercentage = usedPercent,
                DiskHealth = diskHealth,
                LogDirectorySizeBytes = logDirSize,
                DatabaseLogTotalRows = dbRows,
                DatabaseLogHealth = dbHealth,
                ApiChannelDepth = apiDepth,
                EntityChannelDepth = entityDepth,
                OperationChannelDepth = opDepth,
                PendingSpillCount = totalPendingSpill,
                LiveStreamPendingCount = liveStreamDepth,
                PrivilegeSpillCount = totalSpill,
                PrivilegeSpillHealth = spillHealth,
                GovernorCurrentEps = currentEps,
                GovernorDropCount = droppedCount,
                ActiveAlerts = alerts,
                EvaluatedAt = DateTime.UtcNow
            };
        }
    }
}
