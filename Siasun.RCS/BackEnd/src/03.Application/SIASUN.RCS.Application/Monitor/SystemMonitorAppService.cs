using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Settings;

namespace SIASUN.RCS.Monitor
{
    public class SystemMonitorAppService : ApplicationService, ISystemMonitorAppService
    {
        private readonly ISettingProvider _settingProvider;

        public SystemMonitorAppService(ISettingProvider settingProvider)
        {
            _settingProvider = settingProvider;
        }

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
    }
}

