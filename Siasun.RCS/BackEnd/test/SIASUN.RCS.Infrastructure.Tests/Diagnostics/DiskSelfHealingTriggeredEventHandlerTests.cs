using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Diagnostics;
using SIASUN.RCS.Infrastructure.Logging.Diagnostics;
using SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Diagnostics
{
    /// <summary>
    /// 磁盘自愈告警事件订阅处理器单元测试
    /// </summary>
    public class DiskSelfHealingTriggeredEventHandlerTests
    {
        [Fact]
        public async Task HandleEventAsync_ShouldPublishFatalAlertBannerToDiagnosticLiveStream()
        {
            // Arrange
            var mockBroker = Substitute.For<IDiagnosticLiveStreamBroker>();
            mockBroker.IsEnabled.Returns(true);

            var handler = new DiskSelfHealingTriggeredEventHandler(
                NullLogger<DiskSelfHealingTriggeredEventHandler>.Instance,
                mockBroker);

            var evt = new DiskSelfHealingTriggeredEvent(
                preUsagePercent: 88,
                highWatermark: 85,
                lowWatermark: 70,
                message: "工控机磁盘使用率已达 88%，正在紧急强制清理历史日志！");

            // Act
            await handler.HandleEventAsync(evt);

            // Assert
            mockBroker.Received(1).Publish(Arg.Is<LiveEventDto>(dto =>
                dto.Track == DiagnosticTracks.Exception &&
                dto.Level == DiagnosticLevels.Fatal &&
                dto.Source == "DiskSelfHeal" &&
                dto.Title.Contains("88%") &&
                dto.Summary.Contains("工控机磁盘使用率已达 88%")));
        }

        [Fact]
        public async Task DiskSelfHealJob_WhenHighWatermarkTriggered_ShouldPublishDiskSelfHealingTriggeredEvent()
        {
            // Arrange
            var settingProvider = Substitute.For<Volo.Abp.Settings.ISettingProvider>();
            settingProvider.GetOrNullAsync(Monitor.RCSMonitorSettings.IsDiskSelfHealEnabled).Returns(Task.FromResult<string?>("true"));
            // 将高水位阈值设为 0%，确保在任何磁盘使用率下均能触发自愈告警
            settingProvider.GetOrNullAsync(Monitor.RCSMonitorSettings.DiskHighWatermark).Returns(Task.FromResult<string?>("0"));
            settingProvider.GetOrNullAsync(Monitor.RCSMonitorSettings.DiskLowWatermark).Returns(Task.FromResult<string?>("0"));
            settingProvider.GetOrNullAsync(Monitor.RCSMonitorSettings.HardRetentionHours).Returns(Task.FromResult<string?>("0"));

            var mockConfig = Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>();
            var cleanupService = new Infrastructure.AuditLog.Sqlite.AuditLogCleanupService(
                mockConfig,
                NullLogger<Infrastructure.AuditLog.Sqlite.AuditLogCleanupService>.Instance);

            var eventLogRepo = Substitute.For<Volo.Abp.Domain.Repositories.IRepository<Monitor.SystemEventLog, Guid>>();
            var mockEventBus = Substitute.For<Volo.Abp.EventBus.Local.ILocalEventBus>();
            var mockJobContext = Substitute.For<Quartz.IJobExecutionContext>();

            var job = new Infrastructure.BackgroundJobs.DiskSelfHealJob(
                settingProvider,
                cleanupService,
                eventLogRepo,
                NullLogger<Infrastructure.BackgroundJobs.DiskSelfHealJob>.Instance,
                diagnosticLock: null,
                localEventBus: mockEventBus);

            // Act
            await job.Execute(mockJobContext);

            // Assert: 验证 DiskSelfHealJob 成功发布了 DiskSelfHealingTriggeredEvent 事件
            await mockEventBus.Received(1).PublishAsync(Arg.Is<DiskSelfHealingTriggeredEvent>(evt =>
                evt.HighWatermark == 0 &&
                evt.Message.Contains("系统正在紧急强制清理历史分片日志")));
        }
    }
}
