using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Monitor;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Settings;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Monitor
{
    /// <summary>
    /// 系统资源与长期容量监控应用服务单元测试（L4 自治观测）
    /// </summary>
    public class SystemMonitorAppServiceTests
    {
        private readonly ISettingProvider _settingProvider;
        private readonly IRepository<OperationLog, Guid> _opRepo;
        private readonly IRepository<SystemEventLog, Guid> _sysRepo;

        public SystemMonitorAppServiceTests()
        {
            _settingProvider = Substitute.For<ISettingProvider>();
            _settingProvider.GetOrNullAsync(RCSMonitorSettings.IsDiskSelfHealEnabled).Returns(Task.FromResult<string?>("true"));
            _settingProvider.GetOrNullAsync(RCSMonitorSettings.DiskHighWatermark).Returns(Task.FromResult<string?>("85"));
            _settingProvider.GetOrNullAsync(RCSMonitorSettings.DiskLowWatermark).Returns(Task.FromResult<string?>("70"));

            _opRepo = Substitute.For<IRepository<OperationLog, Guid>>();
            _sysRepo = Substitute.For<IRepository<SystemEventLog, Guid>>();
        }

        [Fact]
        public async Task GetSystemResourcesAsync_Should_Return_Disk_And_Memory_Metrics()
        {
            var appService = new SystemMonitorAppService(_settingProvider, _opRepo, _sysRepo);

            var result = await appService.GetSystemResourcesAsync();

            result.ShouldNotBeNull();
            result.Disk.ShouldNotBeNull();
            result.Disk.TotalSizeBytes.ShouldBeGreaterThan(0);
            result.Disk.HighWatermark.ShouldBe(85);
            result.Disk.LowWatermark.ShouldBe(70);
            result.Memory.WorkingSet64.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task GetCapacityHealthAsync_When_Logs_Exceed_Threshold_Should_Trigger_Alert()
        {
            // Arrange: 模拟数据库日志膨胀到 600,000 行（超过 500,000 预警水位）
            _opRepo.GetCountAsync().Returns(Task.FromResult(400_000L));
            _sysRepo.GetCountAsync().Returns(Task.FromResult(200_000L));

            var appService = new SystemMonitorAppService(_settingProvider, _opRepo, _sysRepo);

            // Act
            var report = await appService.GetCapacityHealthAsync();

            // Assert
            report.ShouldNotBeNull();
            report.DatabaseLogTotalRows.ShouldBe(600_000);
            report.DatabaseLogHealth.ShouldBe(CapacityHealthLevel.Warning);
            report.ActiveAlerts.ShouldContain(a => a.Contains("预警水位"));
        }

        [Fact]
        public async Task GetCapacityHealthAsync_When_PrivilegeSpillOccurs_Should_Escalate_To_Critical()
        {
            // Arrange
            _opRepo.GetCountAsync().Returns(Task.FromResult(100L));
            _sysRepo.GetCountAsync().Returns(Task.FromResult(100L));

            var mockApiChannel = Substitute.For<SIASUN.RCS.Auditing.IApiAuditLogChannel>();
            mockApiChannel.SpillCount.Returns(5L);
            mockApiChannel.PendingSpillCount.Returns(2);
            mockApiChannel.TotalQueueCount.Returns(120);

            var mockEntityChannel = Substitute.For<SIASUN.RCS.Auditing.IEntityAuditLogChannel>();
            mockEntityChannel.SpillCount.Returns(3L);
            mockEntityChannel.PendingSpillCount.Returns(1);
            mockEntityChannel.TotalQueueCount.Returns(80);

            var mockOpChannel = Substitute.For<SIASUN.RCS.Auditing.IOperationLogChannel>();
            mockOpChannel.SpillCount.Returns(2L);
            mockOpChannel.PendingSpillCount.Returns(1);
            mockOpChannel.TotalQueueCount.Returns(10);

            var mockLiveStream = Substitute.For<SIASUN.RCS.Diagnostics.ILiveStreamTelemetryProvider>();
            mockLiveStream.PendingCount.Returns(15);

            var mockGovernor = Substitute.For<SIASUN.RCS.Diagnostics.IAdaptiveTrafficGovernor>();
            mockGovernor.GetMetrics().Returns(new SIASUN.RCS.Diagnostics.TrafficGovernorMetrics
            {
                CurrentEps = 150.0,
                CurrentLevel = SIASUN.RCS.Diagnostics.TrafficGovernorLevel.Elevated,
                TotalAdmittedCount = 500,
                TotalDroppedCount = 20
            });

            var appService = new SystemMonitorAppService(
                _settingProvider,
                _opRepo,
                _sysRepo,
                mockApiChannel,
                mockEntityChannel,
                mockOpChannel,
                mockLiveStream,
                mockGovernor);

            // Act
            var report = await appService.GetCapacityHealthAsync();

            // Assert
            report.ShouldNotBeNull();
            report.PrivilegeSpillCount.ShouldBe(10L);
            report.PendingSpillCount.ShouldBe(4);
            report.PrivilegeSpillHealth.ShouldBe(CapacityHealthLevel.Critical);
            report.OverallHealth.ShouldBe(CapacityHealthLevel.Critical);
            report.ApiChannelDepth.ShouldBe(120);
            report.EntityChannelDepth.ShouldBe(80);
            report.OperationChannelDepth.ShouldBe(10);
            report.LiveStreamPendingCount.ShouldBe(15);
            report.GovernorCurrentEps.ShouldBe(150.0);
            report.GovernorDropCount.ShouldBe(20);
            report.ActiveAlerts.ShouldContain(a => a.Contains("应急溢流落盘待消费状态"));
        }

        [Fact]
        public async Task GetCapacityHealthAsync_When_HistoricalSpillRecovered_Should_Mark_Warning()
        {
            // Arrange
            _opRepo.GetCountAsync().Returns(Task.FromResult(100L));
            _sysRepo.GetCountAsync().Returns(Task.FromResult(100L));

            var mockApiChannel = Substitute.For<SIASUN.RCS.Auditing.IApiAuditLogChannel>();
            mockApiChannel.SpillCount.Returns(5L);
            mockApiChannel.PendingSpillCount.Returns(0);

            var mockEntityChannel = Substitute.For<SIASUN.RCS.Auditing.IEntityAuditLogChannel>();
            mockEntityChannel.SpillCount.Returns(0L);
            mockEntityChannel.PendingSpillCount.Returns(0);

            var mockOpChannel = Substitute.For<SIASUN.RCS.Auditing.IOperationLogChannel>();
            mockOpChannel.SpillCount.Returns(0L);
            mockOpChannel.PendingSpillCount.Returns(0);

            var appService = new SystemMonitorAppService(
                _settingProvider,
                _opRepo,
                _sysRepo,
                mockApiChannel,
                mockEntityChannel,
                mockOpChannel);

            // Act
            var report = await appService.GetCapacityHealthAsync();

            // Assert
            report.ShouldNotBeNull();
            report.PrivilegeSpillCount.ShouldBe(5L);
            report.PendingSpillCount.ShouldBe(0);
            report.PrivilegeSpillHealth.ShouldBe(CapacityHealthLevel.Warning);
            ((int)report.OverallHealth).ShouldBeGreaterThanOrEqualTo((int)CapacityHealthLevel.Warning);
            report.ActiveAlerts.ShouldContain(a => a.Contains("当前已全部恢复/入库"));
        }
    }
}
